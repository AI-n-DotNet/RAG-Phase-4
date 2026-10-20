using AInDotNet.RagDemo.Tests.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Evaluation;

public static class RagEvaluationCases
{
    public static RagEvaluationCase Authorization => new()
    {
        Id = "RAG-SECURITY-001",

        Question = "How does enterprise RAG prevent unauthorized access?",

        ExpectedAnswer =
            "Enterprise RAG must enforce authorization " +
            "and must not grant document access solely " +
            "because a document is semantically relevant.",

        ExpectedChunkIds = [69],

        ShouldAbstain = false
    };

    public static RagEvaluationCase AuthorizationImplementation => new()
    {
        Id = "RAG-SECURITY-002",

        Question =
        "What authorization controls are currently implemented " +
        "in our RAG demonstration application?",

        ExpectedAnswer =
        "The available documentation does not establish " +
        "that authorization controls have been implemented " +
        "in the demonstration application.",

        ExpectedChunkIds = [],

        ShouldAbstain = true
    };
}