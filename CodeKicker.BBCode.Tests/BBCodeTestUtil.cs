﻿using System;
using System.Collections.Generic;
using System.Linq;
using CodeKicker.BBCode;
using CodeKicker.BBCode.SyntaxTree;
using Microsoft.Pex.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests2
{
    public static class BBCodeTestUtil
    {
        public static SequenceNode CreateRootNode(BBTag[] allowedTags)
        {
            var node = new SequenceNode();
            AddSubnodes(allowedTags, node);
            return node;
        }
        static SyntaxTreeNode CreateNode(BBTag[] allowedTags, bool allowText)
        {
            switch (PexChoose.ValueFromRange("type", allowText ? 0 : 1, 2))
            {
                case 0:
                    var text = PexChoose.Value<string>("text");
                    PexAssume.IsTrue(!String.IsNullOrEmpty(text));
                    return new TextNode(text);
                case 1:
                    var tag = allowedTags[PexChoose.ValueFromRange("tag", 0, allowedTags.Length)];
                    var node = new TagNode(tag);

                    AddSubnodes(allowedTags, node);

                    if (tag.Attributes != null)
                    {
                        var selectedIds = new List<string>();
                        foreach (var attr in tag.Attributes)
                        {
                            if (!selectedIds.Contains(attr.ID) && PexChoose.Value<bool>("include"))
                            {
                                var val = PexChoose.Value<string>("val");
                                PexAssume.IsTrue(val != null);
                                PexAssume.IsTrue(val.IndexOfAny("[] ".ToCharArray()) == -1);
                                node.AttributeValues[attr] = val;
                                selectedIds.Add(attr.ID);
                            }
                        }
                    }
                    return node;
                default:
                    PexAssume.Fail();
                    return null;
            }
        }
        static void AddSubnodes(BBTag[] allowedTags, SyntaxTreeNode node)
        {
            int count = PexChoose.ValueFromRange("count", 0, 3);
            bool lastWasText = false;
            for (int i = 0; i < count; i++)
            {
                var subNode = CreateNode(allowedTags, !lastWasText);
                lastWasText = subNode is TextNode;
                node.SubNodes.Add(subNode);
            }
        }

        public static BBCodeParser GetParserForTest(ErrorMode errorMode, bool includePlaceholder, BBTagClosingStyle listItemBBTagClosingStyle, bool enableIterationElementBehavior)
        {
            return new BBCodeParser(errorMode, null, new[]
                {
                    new BBTag("b", "<b>", "</b>"), 
                    new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"), 
                    new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"), 
                    new BBTag("code", "<pre class=\"prettyprint\">", "</pre>"), 
                    new BBTag("img", "<img src=\"${content}\" />", "", false, true), 
                    new BBTag("quote", "<blockquote>", "</blockquote>"), 
                    new BBTag("list", "<ul>", "</ul>"), 
                    new BBTag("*", "<li>", "</li>", true, listItemBBTagClosingStyle, null, enableIterationElementBehavior), 
                    new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")), 
                    new BBTag("url2", "<a href=\"${href}\">", "</a>", new BBAttribute("href", "", GetUrl2Href), new BBAttribute("href", "href", GetUrl2Href)), 
                    !includePlaceholder ? null : new BBTag("placeholder", "${name}", "", false, BBTagClosingStyle.LeafElementWithoutContent, null, new BBAttribute("name", "", name => "xxx" + name.AttributeValue + "yyy")), 
                }.Where(x => x != null).ToArray());
        }
        static string GetUrl2Href(IAttributeRenderingContext attributeRenderingContext)
        {
            if (!string.IsNullOrEmpty(attributeRenderingContext.AttributeValue)) return attributeRenderingContext.AttributeValue;

            var content = attributeRenderingContext.GetAttributeValueByID(BBTag.ContentPlaceholderName);
            if (!string.IsNullOrEmpty(content) && content.StartsWith("http:")) return content;

            return null;
        }

        public static BBCodeParser GetSimpleParserForTest(ErrorMode errorMode)
        {
            return new BBCodeParser(errorMode, null, new[]
                {
                    new BBTag("x", "${content}${x}", "${y}", true, true, new BBAttribute("x", "x"), new BBAttribute("y", "y", x => x.AttributeValue)), 
                });
        }

        public static string SimpleBBEncodeForTest(string bbCode, ErrorMode errorMode)
        {
            return GetSimpleParserForTest(errorMode).ToHtml(bbCode);
        }

        public static bool IsValid(string bbCode, ErrorMode errorMode)
        {
            try
            {
                BBCodeParserTest.BBEncodeForTest(bbCode, errorMode);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static SequenceNode GetAnyTree()
        {
            var parser = GetParserForTest(PexChoose.EnumValue<ErrorMode>("errorMode"), true, PexChoose.EnumValue<BBTagClosingStyle>("listItemBBTagClosingStyle"), false);
            return CreateRootNode(parser.Tags.ToArray());
        }
    }
}