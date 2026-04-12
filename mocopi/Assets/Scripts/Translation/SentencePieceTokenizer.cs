using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// opus-mt用 SentencePieceトークナイザ
/// vocab.json（{token: id}形式）を読み込み、最長一致法でトークナイズする
/// </summary>
public class SentencePieceTokenizer
{
    private Dictionary<string, int> _tokenToId = new Dictionary<string, int>();
    private Dictionary<int, string> _idToToken = new Dictionary<int, string>();
    private int _maxTokenLength;
    private bool _loaded;

    //  opus-mt特殊トークン
    public int PadTokenId { get; private set; } = 0;
    public int EosTokenId { get; private set; } = 0;
    public int UnkTokenId { get; private set; } = 0;

    private const string SentencePiecePrefix = "\u2581"; // ▁（U+2581）

    public bool Load(string vocabPath)
    {
        if (!File.Exists(vocabPath))
        {
            Debug.LogError($"[SentencePieceTokenizer] vocab.json が見つかりません: {vocabPath}");
            return false;
        }

        string json = File.ReadAllText(vocabPath);
        _tokenToId = ParseVocabJson(json);

        _idToToken.Clear();
        _maxTokenLength = 0;
        foreach (var kvp in _tokenToId)
        {
            _idToToken[kvp.Value] = kvp.Key;
            if (kvp.Key.Length > _maxTokenLength)
                _maxTokenLength = kvp.Key.Length;
        }

        //  特殊トークンIDを検索
        if (_tokenToId.TryGetValue("</s>", out int eosId)) EosTokenId = eosId;
        if (_tokenToId.TryGetValue("<pad>", out int padId)) PadTokenId = padId;
        if (_tokenToId.TryGetValue("<unk>", out int unkId)) UnkTokenId = unkId;

        _loaded = true;
        Debug.Log($"[SentencePieceTokenizer] ボキャブラリ読み込み完了: {_tokenToId.Count} トークン (EOS={EosTokenId}, PAD={PadTokenId})");
        return true;
    }

    /// <summary>
    /// テキストをトークンIDに変換（最長一致法）
    /// </summary>
    public int[] Encode(string text)
    {
        if (!_loaded) return new int[0];

        var tokens = new List<int>();

        //  SentencePiece形式: 文頭に▁を付加
        string input = SentencePiecePrefix + text;

        int pos = 0;
        while (pos < input.Length)
        {
            //  現在位置から最長一致するトークンを探す
            int bestLen = 0;
            int bestId = UnkTokenId;

            int maxLen = Mathf.Min(_maxTokenLength, input.Length - pos);
            for (int len = maxLen; len >= 1; len--)
            {
                string candidate = input.Substring(pos, len);
                if (_tokenToId.TryGetValue(candidate, out int id))
                {
                    bestLen = len;
                    bestId = id;
                    break;
                }
            }

            if (bestLen == 0)
            {
                //  1文字もマッチしない場合はUNKとして1文字進める
                tokens.Add(UnkTokenId);
                pos++;
            }
            else
            {
                tokens.Add(bestId);
                pos += bestLen;
            }
        }

        //  EOS追加
        tokens.Add(EosTokenId);

        return tokens.ToArray();
    }

    /// <summary>
    /// トークンIDの列をテキストに変換
    /// </summary>
    public string Decode(int[] tokenIds)
    {
        if (!_loaded) return "";

        var sb = new StringBuilder();
        foreach (int id in tokenIds)
        {
            if (id == EosTokenId || id == PadTokenId) continue;

            if (_idToToken.TryGetValue(id, out string token))
            {
                sb.Append(token);
            }
        }

        //  ▁をスペースに変換し、先頭スペースを除去
        string result = sb.ToString().Replace(SentencePiecePrefix, " ");
        return result.Trim();
    }

    private static Dictionary<string, int> ParseVocabJson(string json)
    {
        var dict = new Dictionary<string, int>();

        int i = json.IndexOf('{');
        if (i < 0) return dict;
        i++;

        while (i < json.Length)
        {
            int keyStart = json.IndexOf('"', i);
            if (keyStart < 0) break;
            keyStart++;

            int keyEnd = FindClosingQuote(json, keyStart);
            if (keyEnd < 0) break;

            string key = UnescapeJsonString(json.Substring(keyStart, keyEnd - keyStart));

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
            if (json[i] == '\\') { i++; continue; }
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
