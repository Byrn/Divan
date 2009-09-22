using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace Divan.Lucene
{
    /// <summary>
    /// Unit tests for the Lucene part in Divan. Operates in a separate CouchDB database called divan_lucene_unit_tests.
    /// Requires a working Couchdb-Lucene installation according to Couchdb-Lucene's documentation.
    /// </summary>
    [TestFixture]
    public class CouchLuceneTest
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            server = new CouchServer();
            db = server.GetNewDatabase(DbName);
        }

        [TearDown]
        public void TearDown()
        {
            db.Delete();
        }

        #endregion

        private CouchServer server;
        private CouchDatabase db;
        private const string DbName = "divan_lucene_unit_tests";

        [Test]
        public void ShouldHandleTrivialQuery()
        {
            var design = db.NewDesignDocument("test");
            var view = design.AddLuceneView("simple", @"function (doc) { var ret = new Document(); ret.add(doc.text); return ret;}");
            db.SynchDesignDocuments();

            db.WriteDocument("{\"text\": \"one two three four\"}", "my-funky-id");

            // Hehe, we need to sleep to make sure the indexer catches up... wonder if we can see that somehow?
            Thread.Sleep(5000);

            // Silly query should give no hits
            var result = view.Query().Q("yabbadabba").GetResult();
            var hits = result.Hits();
            Assert.That(hits.Count(), Is.EqualTo(0));

            // Should give one single hit with no included document, but correct id.
            result = view.Query().Q("one").GetResult();
            hits = result.Hits();
            Assert.That(hits.Count(), Is.EqualTo(1));
            Assert.That(hits.First().HasDocument(), Is.False);
            Assert.That(hits.First().Id(), Is.EqualTo("my-funky-id"));

            // Then we should be able to GetDocuments() which will perform a bulk get
			var doc = result.GetDocuments<CouchJsonDocument>().First();
            Assert.That(doc.Id, Is.EqualTo("my-funky-id"));
			Assert.That(doc.Obj["text"].Value<string>(), Is.EqualTo("one two three four"));

            // Then all over again but including documents and getting it out in one single query.
            result = view.Query().Q("one").IncludeDocuments().GetResult();
            doc = result.GetDocuments<CouchJsonDocument>().First();
            Assert.That(doc.Id, Is.EqualTo("my-funky-id"));
            Assert.That(doc.Obj["text"].Value<string>(), Is.EqualTo("one two three four"));
        }
    }
}