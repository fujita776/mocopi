using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Whisper BPEトークナイザ
/// vocab.json からトークンIDとテキストの変換を行う
/// </summary>
public class WhisperTokenizer
{
    //  Whisper特殊トークンID
    public const int EotToken = 50257;            // <|endoftext|>
    public const int StartOfTranscript = 50258;   // <|startoftranscript|>
    public const int JaToken = 50266;             // <|ja|>
    public const int TranscribeToken = 50359;     // <|transcribe|>
    public const int NoTimestampsToken = 50363;   // <|notimestamps|>

    private Dictionary<int, string> _idToToken = new Dictionary<int, string>();
    private Dictionary<string, int> _tokenToId = new Dictionary<string, int>();
    private bool _loaded;

    /// <summary>
    /// 日本語文字起こし用のデコーダ開始トークン列
    /// </summary>
    public int[] DecoderStartTokens => new[]
    {
        StartOfTranscript, JaToken, TranscribeToken, NoTimestampsToken
    };

    /// <summary>
    /// vocab.jsonを読み込む
    /// </summary>
    /// <param name="vocabPath">vocab.jsonのパス</param>
    public bool Load(string vocabPath)
    {
        if (!File.Exists(vocabPath))
        {
            Debug.LogError($"[WhisperTokenizer] vocab.json が見つかりません: {vocabPath}");
            return false;
        }

        string json = File.ReadAllText(vocabPath);
        _tokenToId = ParseVocabJson(json);

        _idToToken.Clear();
        foreach (var kvp in _tokenToId)
        {
            _idToToken[kvp.Value] = kvp.Key;
        }

        _loaded = true;
        Debug.Log($"[WhisperTokenizer] ボキャブラリ読み込み完了: {_tokenToId.Count} トークン");
        return true;
    }

    /// <summary>
    /// トークンIDの列をテキストに変換（デコード）
    /// </summary>
    public string Decode(int[] tokenIds)
    {
        if (!_loaded) return "";

        var sb = new StringBuilder();
        foreach (int id in tokenIds)
        {
            //  特殊トークンはスキップ
            if (id >= 50257) continue;

            if (_idToToken.TryGetValue(id, out string token))
            {
                sb.Append(token);
            }
        }

        //  BPEのバイトレベルエンコーディングをデコード
        string result = DecodeBpeBytes(sb.ToString());
        return result.Trim();
    }

    /// <summary>
    /// Whisper BPEのバイトレベルトークンを実際のテキストに変換
    /// GPT-2スタイルのバイトエンコーディング: Unicode文字をバイト表現に置換
    /// </summary>
    private string DecodeBpeBytes(string bpeText)
    {
        var bytes = new List<byte>();
        var byteMap = GetByteDecoderMap();

        foreach (char c in bpeText)
        {
            if (byteMap.TryGetValue(c, out byte b))
            {
                bytes.Add(b);
            }
        }

        try
        {
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
        catch
        {
            return bpeText;
        }
    }

    /// <summary>
    /// GPT-2 BPEのUnicode→バイトデコーダマップを生成
    /// </summary>
    private static Dictionary<char, byte> GetByteDecoderMap()
    {
        var map = new Dictionary<char, byte>();

        // printable ASCII + Latin-1 supplement の直接マッピング
        // '!' (33) ~ '~' (126), '¡' (161) ~ '¬' (172), '®' (174) ~ 'ÿ' (255)
        int n = 0;
        for (int b = 0; b < 256; b++)
        {
            if ((b >= 33 && b <= 126) || (b >= 161 && b <= 172) || (b >= 174 && b <= 255))
            {
                map[(char)b] = (byte)b;
            }
            else
            {
                //  非printableバイトは256+nにマッピングされている
                map[(char)(256 + n)] = (byte)b;
                n++;
            }
        }

        return map;
    }

    /// <summary>
    /// シンプルなJSONパーサ（vocab.json用、{string: int} 形式）
    /// </summary>
    private static Dictionary<string, int> ParseVocabJson(string json)
    {
        var dict = new Dictionary<string, int>();

        //  {"token": id, "token2": id2, ...} 形式をパース
        int i = json.IndexOf('{');
        if (i < 0) return dict;
        i++;

        while (i < json.Length)
        {
            //  次のキー（ダブルクォート文字列）を探す
            int keyStart = json.IndexOf('"', i);
            if (keyStart < 0) break;
            keyStart++;

            int keyEnd = FindClosingQuote(json, keyStart);
            if (keyEnd < 0) break;

            string key = UnescapeJsonString(json.Substring(keyStart, keyEnd - keyStart));

            //  コロンの後の数値を取得
            int colonIdx = json.IndexOf(':', keyEnd);
            if (colonIdx < 0) break;

            int valueStart = colonIdx + 1;
            while (valueStart < json.Length && (json[valueStart] == ' ' || json[valueStart] == '\t'))
                valueStart++;

            int valueEnd = valueStart;
            while (valueEnd < json.Length && (char.IsDigit(json[valueEnd]) || json[valueEnd] == '-'))
                valueEnd++;

            if (int.TryParse(json.Substring(valueStart, valueEnd - valueStart), out int value))
            {
                dict[key] = value;
            }

            i = valueEnd;
        }

        return dict;
    }

    private static int FindClosingQuote(string json, int start)
    {
        for (int i = start; i < json.Length; i++)
        {
            if (json[i] == '\\')
            {
                i++; //  エスケープ文字をスキップ
                continue;
            }
            if (json[i] == '"') return i;
        }
        return -1;
    }

    private static string UnescapeJsonString(string s)
    {
        return s.Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\/", "/")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
    }
}
