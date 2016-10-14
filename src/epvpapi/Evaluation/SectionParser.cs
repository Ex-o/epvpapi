﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Net;

namespace epvpapi.Evaluation
{
    internal static class SectionParser
    {
        internal class AnnouncementsParser : TargetableParser<Section>, INodeParser
        {
            public AnnouncementsParser(Section target)
                : base(target)
            { }

            public void Execute(HtmlNode coreNode)
            {
                Target.Announcements = new List<Announcement>();

                if (coreNode != null)
                {
                    coreNode = coreNode.SelectSingleNode("tbody");
                    var sectionNodes = new List<HtmlNode>(coreNode.ChildNodes.GetElementsByTagName("tr"));

                    foreach (var announcementNode in sectionNodes.Take(sectionNodes.Count - 1)) // ignore the last node since that is no actual announcement
                    {
                        var announcement = new Announcement(Target);

                        var firstLine = announcementNode.SelectSingleNode("td[2]/div[1]");
                        if (firstLine != null)
                        {
                            var hitsNode = firstLine.SelectSingleNode("span[1]/strong[1]");
                            announcement.Hits = (hitsNode != null) ? (uint)hitsNode.InnerText.To<double>() : 0;

                            var titleNode = firstLine.SelectSingleNode("a[1]");
                            announcement.Title = (titleNode != null) ? titleNode.InnerText : "";
                        }

                        var secondLine = announcementNode.SelectSingleNode("td[2]/div[2]");
                        if (secondLine != null)
                        {
                            var beginNode = secondLine.SelectSingleNode("span[1]/span[1]");
                            if (beginNode != null)
                                announcement.Begins = beginNode.InnerText.ToElitepvpersDateTime();

                            var creatorNode = secondLine.SelectSingleNode("span[2]/a[1]");
                            announcement.Sender.Name = (creatorNode != null) ? WebUtility.HtmlDecode(creatorNode.InnerText) : "";
                            announcement.Sender.ID = creatorNode.Attributes.Contains("href") ? User.FromUrl(creatorNode.Attributes["href"].Value) : 0;
                        }

                        Target.Announcements.Add(announcement);
                    }
                }
            }
        }

        internal class ThreadListingParser : TargetableParser<SectionThread>, INodeParser
        {
            public ThreadListingParser(SectionThread target)
                : base(target)
            { }

            public void Execute(HtmlNode coreNode)
            {
                var previewContentNode = coreNode.SelectSingleNode("td[3]");
                if (previewContentNode != null)
                {
                    if (previewContentNode.InnerText.Contains("Moved")) return; // moved threads do not contain any data to parse
                    Target.PreviewContent = (previewContentNode.Attributes.Contains("title"))
                        ? previewContentNode.Attributes["title"].Value
                        : "";
                }

                var titleNode = coreNode.SelectSingleNode("td[3]/div[1]/a[1]");
                if (titleNode.Id.Contains("thread_gotonew")) // new threads got an additional image displayed (left from the title) wrapped in an 'a' element for quick access to the new reply function
                    titleNode = coreNode.SelectSingleNode("td[3]/div[1]/a[2]");

                Target.InitialPost.Title = (titleNode != null) ? titleNode.InnerText : "";
                Target.ID = (titleNode != null) ? (titleNode.Attributes.Contains("href")) ? SectionThread.FromUrl(titleNode.Attributes["href"].Value) : 0 : 0;

                var threadStatusIconNode = coreNode.SelectSingleNode("td[1]/img[1]");
                Target.Closed = (threadStatusIconNode != null) ? (threadStatusIconNode.Attributes.Contains("src")) ? threadStatusIconNode.Attributes["src"].Value.Contains("lock") : false : false;

                var creatorNode = coreNode.SelectSingleNode("td[3]/div[2]/span[1]");
                if (creatorNode != null)
                {
                    // if the thread has been rated, the element with the yellow stars shows up and is targeted as the first span element
                    // then, the actual node where the information about the creator is stored is located one element below the rating element
                    if (!creatorNode.Attributes.Contains("onclick"))
                        creatorNode = coreNode.SelectSingleNode("td[3]/div[2]/span[2]");

                    Target.Creator = new User(creatorNode.InnerText, creatorNode.Attributes.Contains("onclick") ? User.FromUrl(creatorNode.Attributes["onclick"].Value) : 0);
                }

                var pageNode = coreNode.SelectSingleNode("td[3]/div[1]"); ;
                if (pageNode != null)
                {
                    // span will show if the thread is tagged, got attachments got pages to click on
                    var pageNodes = pageNode.SelectNodes("span");
                    if (pageNodes != null)
                    {
                        // pages are in the last div
                        pageNode = pageNodes.Last();
                        pageNodes = pageNode.SelectNodes("a");
                        if (pageNodes != null)
                        {
                            // last page is in the last a element
                            pageNode = pageNodes.Last();
                            // are we are on the attachments link?
                            if (!pageNode.Attributes.Contains("onclick"))
                            {
                                var match = new Regex("[-]{1}([0-9]*)\\.html").Match(pageNode.GetAttributeValue("href", ""));
                                Target.PageCount = match.Groups[1].Value.To<uint>();
                            }
                        }
                    }
                }

                var repliesNode = coreNode.SelectSingleNode("td[5]/a[1]");
                Target.ReplyCount = (repliesNode != null) ? (uint)repliesNode.InnerText.To<double>() : 0;

                var viewsNode = coreNode.SelectSingleNode("td[6]");
                Target.ViewCount = (viewsNode != null) ? (uint)viewsNode.InnerText.To<double>() : 0;
            }
        }
    }
}
