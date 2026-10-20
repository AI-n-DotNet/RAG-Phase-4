using AInDotNet.RagDemo.Application.Ingestion;
using System;
using System.Collections.Generic;
using System.Text;

namespace AInDotNet.RagDemo.Tests.Ingestion;


[TestClass]
public sealed class TextFileDocumentTests
{
    [TestMethod]
    public async Task LoadAsync_TextFile_PreservesContentAndIdentifiesSource()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, "RAG sample.txt");
        const string content = "SQL Server stores embeddings.\nUnicode example: café.";

        try
        {
            await File.WriteAllTextAsync(path, content);

            TextFileDocument document = await TextFileDocument.LoadAsync(path);

            Assert.AreEqual("RAG sample", document.Title);
            Assert.AreEqual(content, document.Content);
            Assert.AreEqual(new Uri(Path.GetFullPath(path)).AbsoluteUri, document.SourceUri);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}