namespace Microsoft.BotBuilderSamples
{
    public enum GraphAction
    {
        None = 0,
        Profile = 1,
        RecentMail = 2,
        SendTestMail = 3
    }

    public sealed class GraphActionOptions
    {
        public GraphAction Action { get; set; } = GraphAction.None;
    }
}