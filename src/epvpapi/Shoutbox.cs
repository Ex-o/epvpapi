﻿using epvpapi.Connection;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace epvpapi
{
    /// <summary>
    /// Represents the shoutbox accessable by premium users, level2 + level3 users and the staff
    /// </summary>
    public static class Shoutbox
    {
        /// <summary>
        /// Themed chat-channel of the shoutbox where messages can be stored, send and received. 
        /// </summary>
        public class Channel
        {
            /// <summary>
            /// A single shout send by an user
            /// </summary>
            public class Shout
            {
                public User User { get; set; }
                public string Message { get; set; }
                public DateTime Time { get; set; }

                public Shout(User user, string message, DateTime time)
                {
                    User = user;
                    Message = message;
                    Time = time;
                }
            }

            public uint ID { get; set; }
            public string Name { get; set; }

            /// <summary>
            /// List of the most recent shouts available in the channel, updated on executing the <c>Update</c> function
            /// </summary>
            public List<Shout> Shouts { get; set; }

            public Channel(uint id, string name)
            {
                ID = id;
                Name = name;
            }

            /// <summary>
            /// Sends a message to the channel
            /// </summary>
            /// <param name="session"> Session used for sending the request </param>
            /// <param name="message"> The message text to send </param>
            public void Send(Session session, string message)
            {
                session.ThrowIfInvalid();

                Response res = session.Post("http://www.elitepvpers.com/forum/mgc_cb_evo_ajax.php",
                                            new List<KeyValuePair<string, string>>()
                                            {
                                                new KeyValuePair<string, string>("do", "ajax_chat"),
                                                new KeyValuePair<string, string>("channel_id", ID.ToString()),
                                                new KeyValuePair<string, string>("chat", message),
                                                new KeyValuePair<string, string>("securitytoken", session.SecurityToken),
                                                new KeyValuePair<string, string>("s", String.Empty)
                                            });
            }

            /// <summary>
            /// Updates the most recent shouts usually displayed when loading the main page 
            /// </summary>
            /// <param name="session"> Session used for sending the request </param>
            public void Update(Session session)
            {
                Response res = session.Post("http://www.elitepvpers.com/forum/mgc_cb_evo_ajax.php",
                                            new List<KeyValuePair<string, string>>
                                            {
                                                new KeyValuePair<string, string>("do", "ajax_refresh_chat"),
                                                new KeyValuePair<string, string>("status", "open"),
                                                new KeyValuePair<string, string>("channel_id", ID.ToString()),
                                                new KeyValuePair<string, string>("location", "inc"),
                                                new KeyValuePair<string, string>("first_load", "0"),
                                                new KeyValuePair<string, string>("securitytoken", session.SecurityToken),
                                                new KeyValuePair<string, string>("securitytoken", session.SecurityToken), // for some reason, the security token is send twice
                                                new KeyValuePair<string, string>("s", String.Empty),
                                            });

                try
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(res.ToString());

                    // every shoutbox entry got 3 td nodes. One for the time, one for the username and one for the actual messages
                    // the target nodes are identified by their unique valign: top attribute
                    List<HtmlNode> tdNodes = new List<HtmlNode>(doc.DocumentNode.GetElementsByTagName("td"));
                    List<HtmlNode> shoutboxNodes = new List<HtmlNode>(tdNodes.Where(node => node.Attributes.Any(attribute => attribute.Name == "valign" && attribute.Value == "top")));

                    List<List<HtmlNode>> shoutboxNodeGroups = shoutboxNodes.Split(3);
                    
                    Shouts = new List<Shout>();
                    foreach(var shoutboxNodeGroup in shoutboxNodeGroups)
                    {
                        if (shoutboxNodeGroup.Count != 3) continue; // every node group needs to have exactly 3 nodes in order to be valid

                        DateTime time = new DateTime();
                        HtmlNode timeNode = shoutboxNodeGroup.ElementAt(0).SelectSingleNode(@"span[1]/span[1]");

                        if (timeNode != null)
                        {
                            Match match = new Regex(@"\s*(\S+)&nbsp;").Match(timeNode.InnerText);
                            string matchedTime = match.Groups.Count > 1 ? match.Groups[1].Value : String.Empty;
                            DateTime.TryParse(matchedTime, out time);
                        }

                        string username = "";
                        HtmlNode userNameNode = shoutboxNodeGroup.ElementAt(1).SelectSingleNode(@"span[1]/a[1]/span[1]");
                        if (userNameNode != null)
                            username = userNameNode.InnerText;

                        string message = "";
                        HtmlNode messageNode = shoutboxNodeGroup.ElementAt(2).SelectSingleNode(@"span[1]");
                        if (messageNode != null)
                            message = messageNode.InnerText;

                        Shouts.Add(new Shout(new User(username), message, time));
                    }
                }
                catch (HtmlWebException exception)
                {
                    throw new ParsingFailedException("Parsing recent shouts from response content failed", exception);
                }
            }

            /// <summary>
            /// Fetches the history of the specified shoutbox channel and returns all shouts that have been stored
            /// </summary>
            /// <param name="firstPage"> Index of the first page to fetch </param>
            /// <param name="pageCount"> Amount of pages to get. The higher this count, the more data will be generated and received </param>
            /// <param name="session"> Session used for sending the request </param>
            /// <returns> Shouts listed in the channel history that could be obtained and parsed </returns>
            public List<Shout> History(Session session, uint pageCount = 10, uint firstPage = 1)
            {
                session.ThrowIfInvalid();

                List<Shout> shoutList = new List<Shout>();
                for(int i = 0; i < pageCount; ++i)
                {
                    Response res = session.Get("http://www.elitepvpers.com/forum/mgc_cb_evo.php?do=view_archives&page=" + (firstPage + i));
                    
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(res.ToString());

                    HtmlNode messagesRootNode = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/table[2]/tr[2]/td[1]/table[1]/tr[5]/td[1]/table[1]/tr[2]/td[1]/div[1]/div[1]/div[1]/table[1]/tr[1]/td[3]/table[1]");
                    if (messagesRootNode == null) throw new ParsingFailedException("Parsing channel history failed, root node is invalid or was not found");

                    List<HtmlNode> messageNodes = new List<HtmlNode>(messagesRootNode.GetElementsByTagName("tr"));
                    if (messageNodes.Count < 1) throw new ParsingFailedException("Parsing channel history failed, message nodes could not be retrieved");
                    messageNodes.RemoveAt(0); // remove the table header

                    foreach(HtmlNode messageNode in messageNodes)
                    {
                        List<HtmlNode> subNodes = new List<HtmlNode>(messageNode.GetElementsByTagName("td"));
                        if (subNodes.Count != 4) continue; // every message node got exactly 4 subnodes where action, date, user and message are stored

                        HtmlNode dateNode = messageNode.SelectSingleNode("td[2]/span[1]");
                        DateTime time = new DateTime();
                        if (dateNode != null)
                            DateTime.TryParse(dateNode.InnerText, out time);

                        HtmlNode userNode = messageNode.SelectSingleNode("td[3]/span[1]/a[1]/span[1]");
                        string userName = (userNode != null) ? userNode.InnerText : "";

                        HtmlNode textNode = messageNode.SelectSingleNode("td[4]/span[1]");
                        string message = (textNode != null) ? textNode.InnerText.Strip() : "";

                        shoutList.Add(new Shout(new User(userName), message, time));
                    }
                }

                return shoutList;
            }

        };


        /// <summary>
        /// Contains the Top 10 chatters of all channels
        /// </summary>
        public static List<User> TopChatter { get; set; }

        /// <summary>
        /// Amount of messages stored in all shoutbox channels
        /// </summary>
        public static uint MessageCount { get; set; }

        /// <summary>
        /// Amount of messages stored within the last 24 hours in all shoutbox channels
        /// </summary>
        public static uint MessageCountCurrentDay { get; set; }

        private static Channel _Global = new Channel(0, "General");
        public static Channel Global
        {
            get {  return _Global; }
            set { _Global = value; }
        }

        private static Channel _EnglishOnly = new Channel(1, "EnglishOnly");
        public static Channel EnglishOnly
        {
            get { return _EnglishOnly; }
            set { _EnglishOnly = value; }
        }

        /// <summary>
        /// Updates statistics and information about the shoutbox
        /// </summary>
        public static void Update()
        {
            throw new NotImplementedException();
        }
    }
}
