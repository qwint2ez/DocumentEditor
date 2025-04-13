using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;
using Lab2.Interfaces;
using Postgrest;
using Lab2.Documentn;
using Lab2.Enums;

namespace Lab2.Classes
{

    [Table("documents")]
    public class DocumentRecord : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("content")]
        public string Content { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("file_name")]
        public string FileName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SupabaseStorageStrategy : IStorageStrategy
    {
        private readonly Supabase.Client _supabase;

        public SupabaseStorageStrategy()
        {
            _supabase = new Supabase.Client(SupabaseConfig.Url, SupabaseConfig.Key);
            Task.Run(async () => await _supabase.InitializeAsync()).Wait();
        }

        public async Task SaveDocument(DocumentData data, string fileName)
        {
            var record = new DocumentRecord
            {
                Content = data.Content,
                Type = data.Type.ToString(),
                FileName = fileName,
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.From<DocumentRecord>().Insert(record);
        }

        public async Task<DocumentData> LoadDocument(string fileName)
        {
            var response = await _supabase.From<DocumentRecord>()
                .Where(x => x.FileName == fileName)
                .Get();

            var record = response.Models.FirstOrDefault();
            if (record == null)
                throw new FileNotFoundException("Document not found in storage");

            return new DocumentData
            {
                Content = record.Content,
                Type = Enum.Parse<DocumentType>(record.Type)
            };
        }
    }
}
