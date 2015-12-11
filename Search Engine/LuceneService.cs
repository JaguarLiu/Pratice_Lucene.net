using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Search_Engine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Version = Lucene.Net.Util.Version;
namespace Search_Engine
{
    public static class LuceneService
    {
        public static string _luceneDir = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "Search_index");
        private static FSDirectory _directoryTemp;
        private static FSDirectory _directory
        {
            get
            {
                if (_directoryTemp == null) _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                if (IndexWriter.IsLocked(_directoryTemp)) IndexWriter.Unlock(_directoryTemp);
                var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                if (File.Exists(lockFilePath)) File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }
        public static void Init()
        {
            var raw = new List<Product> {
                           new Product {Id = 1, Name = "林鳳營", Description = "濃醇香"},
                           new Product {Id = 2, Name = "統一", Description = "台灣最大"},
                           new Product {Id = 3, Name = "義美", Description = "最後良心"},
                           new Product {Id = 4, Name = "泰山", Description = "老字號"},
                           new Product {Id = 5, Name = "黑松", Description = "只剩沙士"}
                         };
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            // add data to lucene search index (replaces older entries if any)
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var r in raw)
                {
                    _buildLuceneIndex(r, writer, "Id");
                }
                writer.Optimize();
                // close handles    
                analyzer.Close();
                writer.Dispose();
            }
        }
        public static List<T> Search<T>(string searchQuery) where T : class,new()
        {
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<T>();
            // set up lucene searcher
            using (var searcher = new IndexSearcher(_directory, false))
            {
                var hits_limit = 1000;
                var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                var type = typeof(T);
                var fields = new List<string>();
                foreach (var p in type.GetProperties())
                {
                    fields.Add(p.Name);
                }
                var parser = new MultiFieldQueryParser
                        (Version.LUCENE_30, fields.ToArray(), analyzer);
                var query = parser.Parse(searchQuery);
                var hits = searcher.Search(query, null, hits_limit, Sort.INDEXORDER).ScoreDocs;
                var results = _mappingToDataList<T>(hits, searcher);
                analyzer.Close();
                searcher.Dispose();
                return results;
            }
        }
        private static List<T> _mappingToDataList<T>(IEnumerable<ScoreDoc> hits, IndexSearcher searcher) where T: class,new()
        {
            return hits.Select(hit => _mappingDocumentToData<T>(searcher.Doc(hit.Doc))).ToList();
        }
        private static T _mappingDocumentToData<T>(Document doc) where T : class , new()
        {
            var mappingObj = new T();
            var type = typeof(T);
            foreach (var p in type.GetProperties())
            {
                if (p.PropertyType == typeof(Int32))
                {
                    p.SetValue(mappingObj, int.Parse(doc.Get(p.Name)));
                }
                else
                {
                    p.SetValue(mappingObj, doc.Get(p.Name));
                }
            }
            return mappingObj;
        }
        private static void _buildLuceneIndex<T>(T rawData, IndexWriter writer,string uniqueKey) where T :class
        {
            // add new index entry
            var doc = new Document();
            
            var type = typeof(T);
            // remove older index entry
            var searchQuery = new TermQuery(new Term(uniqueKey, type.GetProperty(uniqueKey).GetValue(rawData).ToString()));
            writer.DeleteDocuments(searchQuery);
            // add lucene fields mapped to db fields
            foreach (var p in type.GetProperties())
            {
                doc.Add(new Field(p.Name, p.GetValue(rawData).ToString(),
                    Field.Store.YES, 
                    p.Name == uniqueKey ? Field.Index.NOT_ANALYZED : Field.Index.ANALYZED));
            }
            // add entry to index
            writer.AddDocument(doc);
            
        }
    }
}