</head>
<body>

  <h1>TwitchAPI</h1>
  <h3>A Lightweight Static Class for Unity Twitch Integration</h3>
  <p align="center">
    TwitchAPI enables seamless integration with Twitch by using only an <strong>ACCESS TOKEN</strong>.
    It connects to Twitch's IRC and Helix API endpoints, validates the token, retrieves user information,
    and processes chat and subscription messages. It also provides methods to fetch a random subset of chatters
    and the current viewer count.
  </p>

  <hr />

  <h2>About The Project</h2>
  <p>
    TwitchAPI is a static utility class for Unity projects that allows you to:
  </p>
  <ul>
    <li>Connect with Twitch using a single ACCESS TOKEN.</li>
    <li>Establish an IRC connection to receive live chat messages.</li>
    <li>Automatically detect subscription events.</li>
    <li>Retrieve a randomized list of chatters.</li>
    <li>Get the current viewer count of your live stream.</li>
  </ul>
  <p>
    All features are accessible from anywhere in your Unity project without needing to instantiate any objects.
  </p>

  <hr />

  <h2>Features</h2>
  <ul>
    <li><strong>Single-Token Connection:</strong> Validate and connect using only an ACCESS TOKEN. The token is used to retrieve the user's login, numeric ID, and client ID.</li>
    <li><strong>IRC Chat Integration:</strong> Connect to Twitch's IRC server to receive live chat messages directly in Unity.</li>
    <li><strong>Subscription Detection:</strong> Automatically detect and trigger events when a user subscribes.</li>
    <li><strong>Helix API Methods:</strong>
      <ul>
        <li>Fetch a randomized list of current chatters.</li>
        <li>Retrieve the current viewer count of your live stream.</li>
      </ul>
    </li>
    <li><strong>Static Utility:</strong> Easily accessible from anywhere in your project without requiring object instantiation.</li>
  </ul>

  <hr />

  <h2>Installation</h2>
  <ol>
    <li><strong>Clone or download</strong> the repository.</li>
    <li><strong>Copy the <code>TwitchAPI.cs</code> file</strong> into your Unity project's Assets folder.</li>
    <li><strong>Add a MonoBehaviour script</strong> to your scene that calls the <code>TwitchAPI.Update()</code> method in your <code>Update()</code> loop.</li>
  </ol>

  <hr />

  <h2>Usage</h2>

  <h3>Connecting to Twitch</h3>
  <p>
    Call the <code>TwitchAPI.ConnectToTwitch(accessToken)</code> method with a valid Twitch ACCESS TOKEN.
    The token is automatically validated to retrieve user information and establish the IRC connection.
  </p>
  <pre><code>
// Example usage in a MonoBehaviour
public class TwitchManager : MonoBehaviour
{
    public string accessToken;

    private void Start()
    {
        TwitchAPI.OnSendMessage += HandleChatMessage;
        TwitchAPI.OnSubscription += HandleSubscription;
        TwitchAPI.ConnectToTwitch(accessToken);
    }

    private void Update()
    {
        TwitchAPI.Update();
    }

    private void HandleChatMessage(string nick, string message)
    {
        Debug.Log($"Chat message from {nick}: {message}");
    }

    private void HandleSubscription(string nick, string message)
    {
        Debug.Log($"Subscription from {nick}: {message}");
    }
}
  </code></pre>

  <h3>Disconnecting</h3>
  <p>
    To disconnect from Twitch, simply call:
  </p>
  <pre><code>TwitchAPI.DisconnectToTwitch();</code></pre>

  <h3>Additional Methods</h3>
  <ul>
    <li><strong>Get Random Chatters:</strong>
      <pre><code>List&lt;string&gt; randomChatters = TwitchAPI.GetRandomNickFromChat(5);</code></pre>
    </li>
    <li><strong>Get Viewer Count:</strong>
      <pre><code>int viewers = TwitchAPI.GetViewerCount();</code></pre>
    </li>
  </ul>

  <hr />

  <h2>Connect With Me</h2>
  <p align="left">
    <a href="https://github.com/yourusername" target="_blank">GitHub</a> •
    <a href="https://twitter.com/yourtwitter" target="_blank">Twitter</a>
  </p>

  <hr />

  <h2>License</h2>
  <p>
    This project is open source. Feel free to use and modify it according to your needs.
  </p>

</body>
</html>
