using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace StreamMate.API
{
    [Serializable]
    public class TokenValidationResponse
    {
        public string client_id;
        public string login;
        public List<string> scopes;
        public string user_id;
    }

    public static class TwitchAPI
    {
        // Public events for external subscription.
        public static Action<string, string> OnSendMessage;
        public static Action OnDisconnect;
        public static Action<string, string> OnSubscription;
        public static Action OnFirstConnect; // Called when first connected

        // IRC connection fields.
        private static TcpClient _twitch;
        private static StreamReader _reader;
        private static StreamWriter _writer;

        private const string IrcUrl = "irc.chat.twitch.tv";
        private const int IrcPort = 6667;

        // User and connection information.
        private static string user;           // Username (login)
        private static string userId;         // Numeric user ID
        private static string authentication; // ACCESS TOKEN
        private static string channel;
        private static string debug;

        // Helix Client ID obtained from token validation.
        private static string helixClientId;

        // Ping timer.
        private static float _pingCounter = 0f;

        public static bool IsConnected => _twitch != null && _twitch.Connected;

        /// <summary>
        /// Validates the ACCESS TOKEN and retrieves the user's login, numeric ID, and client_id,
        /// then establishes the IRC connection.
        /// </summary>
        /// <param name="accessToken">User's ACCESS TOKEN</param>
        public static void ConnectToTwitch(string accessToken)
        {
            authentication = accessToken;
            // Validate the token and retrieve user information.
            TokenValidationResponse validation = ValidateToken(authentication);
            if (validation != null)
            {
                user = validation.login;
                userId = validation.user_id;
                helixClientId = validation.client_id; // Obtain the Helix Client ID from the token validation.
                channel = user; // The channel name is assumed to be the same as the user's login.
                Debug.Log("User validated: " + user + " (" + userId + "), ClientId: " + helixClientId);
                OnFirstConnect?.Invoke();
                Connect();
            }
            else
            {
                Debug.LogError("Token validation failed.");
            }
        }

        /// <summary>
        /// Disconnects from Twitch.
        /// </summary>
        public static void DisconnectToTwitch()
        {
            if (_twitch != null)
            {
                _twitch.Dispose();
                _twitch = null;
            }
            authentication = string.Empty;
            user = string.Empty;
            channel = string.Empty;
            OnDisconnect?.Invoke();
        }

        /// <summary>
        /// Should be called periodically (e.g., from a MonoBehaviour Update method) to handle pinging and message processing.
        /// </summary>
        public static void Update()
        {
            if (string.IsNullOrEmpty(authentication) || string.IsNullOrEmpty(user) || _twitch == null)
                return;

            _pingCounter += Time.deltaTime;
            if (_pingCounter > 60f)
            {
                _writer.WriteLine("PING " + IrcUrl);
                _writer.Flush();
                _pingCounter = 0f;
            }
            if (!_twitch.Connected)
            {
                Connect();
                return;
            }
            if (_twitch.Available <= 0)
                return;

            string message = _reader.ReadLine();
            if (string.IsNullOrEmpty(message))
                return;

            // Check for subscription messages (USERNOTICE with msg-id=sub).
            if (message.Contains("USERNOTICE") && message.Contains("msg-id=sub"))
            {
                int exclamationIndex = message.IndexOf("!");
                if (exclamationIndex > 1)
                {
                    string subUser = message.Substring(1, exclamationIndex - 1);
                    Debug.Log("Subscriber: " + subUser);
                    OnSubscription?.Invoke(subUser, message);
                    return;
                }
            }

            // Process normal chat messages.
            if (message.Contains("Welcome"))
            {
                OnFirstConnect?.Invoke();
            }
            if (!message.Contains("PRIVMSG"))
                return;

            int splitPoint = message.IndexOf("!", StringComparison.Ordinal);
            string nickName = message.Substring(1, splitPoint - 1);
            splitPoint = message.IndexOf(":", 2, StringComparison.Ordinal);
            string content = message.Substring(splitPoint + 1);
            OnSendMessage?.Invoke(nickName, content);
        }

        /// <summary>
        /// Establishes the IRC connection.
        /// </summary>
        private static void Connect()
        {
            _twitch = new TcpClient(IrcUrl, IrcPort);
            _reader = new StreamReader(_twitch.GetStream());
            _writer = new StreamWriter(_twitch.GetStream());

            // For the IRC connection, prepend "oauth:" to the token.
            _writer.WriteLine("PASS oauth:" + authentication);
            _writer.WriteLine("NICK " + user.ToLower());
            _writer.WriteLine("JOIN #" + channel.ToLower());
            _writer.Flush();
        }

        /// <summary>
        /// Retrieves the list of chatters from the Helix API and returns a random subset of usernames.
        /// </summary>
        /// <param name="count">Number of random usernames to return.</param>
        /// <returns>List of randomly selected usernames.</returns>
        public static List<string> GetRandomNickFromChat(int count)
        {
            List<string> randomNicks = new List<string>();
            List<string> allChatters = new List<string>();

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("User ID not obtained.");
                return randomNicks;
            }

            string url = $"https://api.twitch.tv/helix/chat/chatters?broadcaster_id={userId}&moderator_id={userId}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + authentication);
            request.Headers.Add("Client-Id", helixClientId);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    ChattersResponse chattersResponse = JsonUtility.FromJson<ChattersResponse>(json);
                    if (chattersResponse != null && chattersResponse.data != null)
                    {
                        foreach (var chatter in chattersResponse.data)
                        {
                            allChatters.Add(chatter.user_login);
                            Debug.Log("Chatter: " + chatter.user_login);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error fetching chatters list: " + ex);
                return randomNicks;
            }

            if (allChatters.Count == 0)
            {
                Debug.LogWarning("No chatters found in the chat.");
                return randomNicks;
            }

            // Randomize the list using the Fisher-Yates algorithm.
            System.Random rand = new System.Random();
            int n = allChatters.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                var temp = allChatters[k];
                allChatters[k] = allChatters[n];
                allChatters[n] = temp;
            }

            int takeCount = Math.Min(count, allChatters.Count);
            for (int i = 0; i < takeCount; i++)
            {
                randomNicks.Add(allChatters[i]);
            }

            return randomNicks;
        }

        /// <summary>
        /// Returns the viewer count of the live stream.
        /// If the stream is not live, returns 0.
        /// </summary>
        /// <returns>The viewer count.</returns>
        public static int GetViewerCount()
        {
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("User ID not obtained.");
                return 0;
            }

            string url = $"https://api.twitch.tv/helix/streams?user_id={userId}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + authentication);
            request.Headers.Add("Client-Id", helixClientId);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    StreamsResponse streamsResponse = JsonUtility.FromJson<StreamsResponse>(json);
                    if (streamsResponse != null && streamsResponse.data != null && streamsResponse.data.Count > 0)
                    {
                        return streamsResponse.data[0].viewer_count;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error fetching viewer count: " + ex);
            }
            return 0;
        }

        /// <summary>
        /// Validates the ACCESS TOKEN by querying Twitch's validation endpoint and retrieves user information.
        /// </summary>
        /// <param name="token">The ACCESS TOKEN.</param>
        /// <returns>A TokenValidationResponse object or null if validation fails.</returns>
        private static TokenValidationResponse ValidateToken(string token)
        {
            string url = "https://id.twitch.tv/oauth2/validate";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            // The validation endpoint requires the Authorization header in this format.
            request.Headers.Add("Authorization", "OAuth " + token);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    TokenValidationResponse validation = JsonUtility.FromJson<TokenValidationResponse>(json);
                    return validation;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Token validation error: " + ex);
                return null;
            }
        }
    }

    [Serializable]
    public class ChatterData
    {
        public string user_id;
        public string user_login;
        public string user_name;
    }

    [Serializable]
    public class Pagination
    {
        public string cursor;
    }

    [Serializable]
    public class ChattersResponse
    {
        public List<ChatterData> data;
        public Pagination pagination;
        public int total;
    }

    [Serializable]
    public class StreamData
    {
        public int viewer_count;
    }

    [Serializable]
    public class StreamsResponse
    {
        public List<StreamData> data;
        public Pagination pagination;
    }

    [Serializable]
    public class UserData
    {
        public string id;
        public string login;
        public string display_name;
    }

    [Serializable]
    public class UsersResponse
    {
        public List<UserData> data;
    }
}
