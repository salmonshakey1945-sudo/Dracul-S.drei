using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Dracul.UI
{
    /// <summary>
    /// メッセージウィンドウ（最新ログが一番下、最大7行表示、右下配置対応）を制御するスクリプト。
    /// MessageLogUI.AddLog(メッセージ) でどこからでもログを追加できます。
    /// </summary>
    public class MessageLogUI : MonoBehaviour
    {
        public static MessageLogUI Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("メッセージを表示するTextMeshPro Text")]
        [SerializeField] private TMP_Text _messageText;

        [Header("Settings")]
        [Tooltip("画面に表示する最大行数（デフォルト7行）")]
        [SerializeField] private int _maxLines = 7;

        [Tooltip("メッセージの自動消去を行うかどうか")]
        [SerializeField] private bool _autoClear = false;

        [Tooltip("自動消去までの秒数（_autoClearがtrueの時のみ）")]
        [SerializeField] private float _clearDelay = 5f;

        [Header("Debug")]
        [Tooltip("テスト用メッセージ")]
        [SerializeField] private string _testMessage = "テストメッセージ";

        // メッセージを保持するキュー（最大 _maxLines 件）
        private readonly Queue<string> _messages = new Queue<string>();
        private float _lastMessageTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_messageText == null)
            {
                _messageText = GetComponentInChildren<TMP_Text>();
            }

            UpdateDisplay();
        }

        private void Update()
        {
            // 自動消去の処理
            if (_autoClear && _messages.Count > 0)
            {
                if (Time.time - _lastMessageTime > _clearDelay)
                {
                    _messages.Dequeue();
                    _lastMessageTime = Time.time;
                    UpdateDisplay();
                }
            }
        }

        /// <summary>
        /// どこからでも呼び出せる静的ログ追加メソッド
        /// 例: MessageLogUI.AddLog("アイテムを取得しました！");
        /// </summary>
        public static void AddLog(string message)
        {
            if (Instance != null)
            {
                Instance.AddMessage(message);
            }
            else
            {
                Debug.Log($"[MessageLog] {message}");
            }
        }

        /// <summary>
        /// メッセージを1行追加する（最新が最下行になる）
        /// </summary>
        public void AddMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // 改行が含まれている場合は1行ずつ分割して追加
            string[] lines = message.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            foreach (var line in lines)
            {
                // 最大行数を超えたら一番古いメッセージ（最上部）を破棄
                while (_messages.Count >= _maxLines)
                {
                    _messages.Dequeue();
                }

                _messages.Enqueue(line);
            }

            _lastMessageTime = Time.time;
            UpdateDisplay();
        }

        /// <summary>
        /// メッセージログをすべてクリアする
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            UpdateDisplay();
        }

        /// <summary>
        /// テキスト表示を更新する
        /// </summary>
        private void UpdateDisplay()
        {
            if (_messageText == null) return;

            // キュー内のメッセージを改行で連結
            _messageText.text = string.Join("\n", _messages);
        }

        [ContextMenu("Send Test Message")]
        public void SendTestMessage()
        {
            AddMessage($"{_testMessage} ({System.DateTime.Now:HH:mm:ss})");
        }
    }
}
