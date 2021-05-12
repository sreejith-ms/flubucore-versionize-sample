using System.Text.Json.Serialization;

namespace BuildScript.Test
{
    public class Class1
    {
        public static int TestA = 22;

        [JsonPropertyName("scopeName")]
        public int A { get; set; }
    }
}
