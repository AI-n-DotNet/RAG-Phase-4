using AInDotNet.RagDemo.Evaluation;
using Microsoft.Extensions.AI;

namespace AInDotNet.RagDemo.Tests.Evaluation;

[TestClass]
[TestCategory("Deterministic")]
public sealed class LlmCitedClaimRepairerTests
{
    [TestMethod]
    public async Task RepairAsync_IndexedResponse_PreservesOrderAndRemovesMarkers()
    {
        using var client = new CountingChatClient(
            """
            [
                {"claimIndex": 1, "claim": "The sources do not confirm implementation."},
                {"claimIndex": 0, "claim": "They indicate that authorization is required [S1]."}
            ]
            """);

        var repairer = new LlmCitedClaimRepairer(client);

        var result = await repairer.RepairAsync(
            [Claim("Authorization is required."), Claim("Implementation is confirmed.")]);

        Assert.AreEqual(1, client.Calls);
        CollectionAssert.AreEqual(
            new[]
            {
                "They indicate that authorization is required.",
                "The sources do not confirm implementation."
            },
            result.ToArray());
    }

    [TestMethod]
    public async Task RepairAsync_MissingReplacement_Throws()
    {
        var repairer = Create("[]");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repairer.RepairAsync([Claim("Rejected.")]));
    }

    [TestMethod]
    public async Task RepairAsync_DuplicateIndex_Throws()
    {
        var repairer = Create(
            """[{"claimIndex":0,"claim":"First."},{"claimIndex":0,"claim":"Second."}]""");

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repairer.RepairAsync([Claim("First rejected."), Claim("Second rejected.")]));

        StringAssert.Contains(exception.Message, "claim index");
    }

    [TestMethod]
    public async Task RepairAsync_UnknownIndex_Throws()
    {
        var repairer = Create("""[{"claimIndex":3,"claim":"Replacement."}]""");

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repairer.RepairAsync([Claim("Rejected.")]));

        StringAssert.Contains(exception.Message, "claim index");
    }

    [TestMethod]
    public async Task RepairAsync_UnrepairableClaim_Throws()
    {
        var repairer = Create("""[{"claimIndex":0,"claim":null}]""");

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repairer.RepairAsync([Claim("Rejected.")]));

        StringAssert.Contains(exception.Message, "unrepairable");
    }

    [TestMethod]
    public async Task RepairAsync_InvalidJson_Throws()
    {
        var repairer = Create("not JSON");

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repairer.RepairAsync([Claim("Rejected.")]));

        Assert.AreEqual("Claim repair returned invalid JSON.", exception.Message);
    }

    [TestMethod]
    public async Task RepairAsync_NonArrayResponse_Throws()
    {
        var repairer = Create("""{"claimIndex":0,"claim":"Replacement."}""");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repairer.RepairAsync([Claim("Rejected.")]));
    }

    [TestMethod]
    public async Task RepairAsync_EmptyInput_SkipsModelResponse()
    {
        using var client = new CountingChatClient("not JSON");
        var repairer = new LlmCitedClaimRepairer(client);

        var result = await repairer.RepairAsync([]);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, client.Calls);
    }

    [TestMethod]
    public async Task RepairAsync_Cancelled_StopsBeforeModelResponse()
    {
        using var client = new CountingChatClient("not JSON");
        var repairer = new LlmCitedClaimRepairer(client);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => repairer.RepairAsync([Claim("Rejected.")], cancellation.Token));

        Assert.AreEqual(0, client.Calls);
    }

    private static LlmCitedClaimRepairer Create(string response) =>
        new(new FakeChatClient(response));

    private static CitedClaim Claim(string text) => new()
    {
        Claim = text,
        OriginalText = "They indicate that authorization is required, but do not confirm implementation.",
        ContextText = "They indicate that authorization is required, but do not confirm implementation.",
        CitationLabels = ["S1"]
    };

    private sealed class CountingChatClient(string response) : IChatClient
    {
        private readonly FakeChatClient _inner = new(response);
        public int Calls { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return _inner.GetResponseAsync(messages, options, cancellationToken);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() => _inner.Dispose();
    }
}
