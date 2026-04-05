using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// opus-mt用 簡易SentencePieceトークナイザ
/// vocab.json（{token: id}形式）を読み込み、テキストのトークナイズ/デトークナイズを行う
/// </summary>
public class SentencePieceTokenizer
{
    private Dictionary<string, int> _tokenToId = new Dictionary<string, int>();
    private Dictionary<int, string> _idToToken = new Dictionary<int, string>();
    private bool _loaded;

    //  opus-mt特殊トークン
    public int PadTokenId { get; private set; } = 0;
    public int EosTokenId { get; private set; } = 0;
    public int UnkTokenId { get; private set; } = 0;

    private const string SentencePiecePrefix = "\u2581"; // ▁（U+2581）

    /// <summary>
    /// vocab.jsonを読み込む
    /// </summary>
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
        foreach (var kvp in _tokenToId)
        {
            _idToToken[kvp.Value] = kvp.Key;
        }

        //  特殊トークンIDを検索
        if (_tokenToId.TryGetValue("</s>", out int eosId)) EosTokenId = eosId;
        if (_tokenToId.TryGetValue("<pad>", out int padId)) PadTokenId = padId;
        if (_tokenToId.TryGetValue("<unk>", out int unkId)) UnkTokenId = unkId;

        _loaded = true;
        Debug.Log($"[SentencePieceTokenizer] ボキャブラリ読み込み完了: {_tokenToId.Count} トークン (EOS={EosTokenId})");
        return true;
    }

    /// <summary>
    /// テキストをトークンIDに変換
    /// 簡易実装: スペース区切り + ▁プレフィックス
    /// </summary>
    public int[] Encode(string text)
    {
        if (!_loaded) return new int[0];

        var tokens = new List<int>();

        //  テキストをスペースで分割し、各単語に▁プレフィックスを付ける
        string[] words = text.Split(' ');
        for (int w = 0; w < words.Length; w++)
        {
            string word = words[w].Trim();
            if (string.IsNullOrEmpty(word)) continue;

            //  先頭単語には▁を付ける
            string prefixedWord = SentencePiecePrefix + word;

            //  ボキャブラリで完全一致を試みる
            if (_tokenToId.TryGetValue(prefixedWord, out int fullId))
            {
                tokens.Add(fullId);
                continue;
            }

            //  文字単位でフォールバック
            foreach (char c in prefixedWord)
            {
                string charStr = c.ToString();
                if (_tokenToId.TryGetValue(charStr, out int charId))
                {
                    tokens.Add(charId);
                }
                else
                {
                    tokens.Add(UnkTokenId);
                }
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
            //  特殊トークンをスキップ
            if (id == EosTokenId || id == PadTokenId) continue;

            if (_idToToken.TryGetValue(id, out string token))
            {
                sb.Append(token);
            }
        }

        //  ▁をスペースに変換
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
