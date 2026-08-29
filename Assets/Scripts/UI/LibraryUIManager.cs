using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Dracul.Item;
using Dracul.Player;

namespace Dracul.UI
{
    /// <summary>
    /// 情報ライブラリ（Library）ウィンドウの開閉・表示を管理するマネージャー。
    /// Vキーでウィンドウを表示/非表示し、左側で選択したInfomationの文章を右側に表示する。
    /// </summary>
    public class LibraryUIManager : MonoBehaviour
    {
        public static LibraryUIManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("ライブラリUIのルートパネル（縦長ウィンドウ）")]
        public GameObject libraryPanel;

        [Tooltip("プレイヤーの PlayerLibrary コンポーネント（空なら自動取得）")]
        public PlayerLibrary playerLibrary;

        [Tooltip("StarterAssetsInputs コンポーネント（カーソル制御用）")]
        public StarterAssets.StarterAssetsInputs starterAssetsInputs;

        [Header("Left List (Selection)")]
        [Tooltip("情報一覧ボタンを並べるコンテナ（Content Transform）")]
        public Transform listContainer;

        [Tooltip("情報一覧ボタンのPrefab（LibraryItemButton付き）")]
        public GameObject itemButtonPrefab;

        [Tooltip("未解放（0件）の際に表示するテキスト/オブジェクト（省略可）")]
        public GameObject emptyListMessage;

        [Header("Right Detail (Content Display)")]
        [Tooltip("詳細文章を表示する親オブジェクト")]
        public GameObject detailPanel;

        [Tooltip("情報のタイトルテキスト")]
        public TextMeshProUGUI detailTitleText;

        [Tooltip("情報の本文テキスト（スクロール対応）")]
        public TextMeshProUGUI detailContentText;

        [Tooltip("情報のアイコン画像（省略可）")]
        public Image detailIconImage;

        [Tooltip("未選択時に表示するプレースホルダー（省略可）")]
        public GameObject emptyDetailMessage;

        [Header("Controls")]
        [Tooltip("ウィンドウを閉じるボタン（省略可）")]
        public Button closeButton;

        private bool _isOpen = false;
        private ItemData _currentSelectedItem;
        private readonly List<LibraryItemButton> _spawnedButtons = new List<LibraryItemButton>();

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

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseLibrary);
            }
        }

        private void Start()
        {
            if (libraryPanel != null)
            {
                libraryPanel.SetActive(false);
            }
            _isOpen = false;

            if (playerLibrary == null)
            {
                playerLibrary = PlayerLibrary.Instance ?? FindObjectOfType<PlayerLibrary>();
            }

            if (playerLibrary != null)
            {
                playerLibrary.OnLibraryUpdated += OnLibraryUpdated;
            }
        }

        private void OnDestroy()
        {
            if (playerLibrary != null)
            {
                playerLibrary.OnLibraryUpdated -= OnLibraryUpdated;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Vキーでトグル開閉
            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                if (_isOpen)
                {
                    CloseLibrary();
                }
                else
                {
                    OpenLibrary();
                }
            }

            // ESCキーで閉じる
            if (_isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseLibrary();
            }
        }

        /// <summary>
        /// ライブラリウィンドウを開く
        /// </summary>
        public void OpenLibrary()
        {
            _isOpen = true;

            if (libraryPanel != null)
            {
                libraryPanel.SetActive(true);
            }

            // マウスカーソルを表示し、カメラ操作を無効化
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.cursorInputForLook = false;
                starterAssetsInputs.cursorLocked = false;
            }

            RefreshList();
        }

        /// <summary>
        /// ライブラリウィンドウを閉じる
        /// </summary>
        public void CloseLibrary()
        {
            _isOpen = false;

            if (libraryPanel != null)
            {
                libraryPanel.SetActive(false);
            }

            // マウスカーソルを非表示に戻し、カメラ操作を再有効化
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.cursorInputForLook = true;
                starterAssetsInputs.cursorLocked = true;
            }
        }

        /// <summary>
        /// ライブラリが開いているかどうか
        /// </summary>
        public bool IsOpen => _isOpen;

        private void OnLibraryUpdated()
        {
            if (_isOpen)
            {
                RefreshList();
            }
        }

        /// <summary>
        /// 左側リストを再構築し、詳細画面を更新する
        /// </summary>
        public void RefreshList()
        {
            // 既存ボタンの破棄
            foreach (var btn in _spawnedButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _spawnedButtons.Clear();

            if (playerLibrary == null)
            {
                playerLibrary = PlayerLibrary.Instance ?? FindObjectOfType<PlayerLibrary>();
            }

            var unlockedItems = playerLibrary != null ? playerLibrary.UnlockedItems : null;
            bool hasItems = unlockedItems != null && unlockedItems.Count > 0;

            if (emptyListMessage != null)
            {
                emptyListMessage.SetActive(!hasItems);
            }

            if (!hasItems)
            {
                ShowDetail(null);
                return;
            }

            // アイテムボタンの生成
            ItemData targetSelection = _currentSelectedItem;
            bool foundPreviousSelection = false;

            for (int i = 0; i < unlockedItems.Count; i++)
            {
                var item = unlockedItems[i];
                if (item == null) continue;

                if (item == targetSelection)
                {
                    foundPreviousSelection = true;
                }

                if (itemButtonPrefab != null && listContainer != null)
                {
                    var go = Instantiate(itemButtonPrefab, listContainer);
                    var btn = go.GetComponent<LibraryItemButton>();
                    if (btn != null)
                    {
                        btn.Setup(item, SelectItem);
                        _spawnedButtons.Add(btn);
                    }
                }
            }

            // 選択アイテムが未決定、または存在しない場合は先頭を選択
            if (!foundPreviousSelection || targetSelection == null)
            {
                targetSelection = unlockedItems[0];
            }

            SelectItem(targetSelection);
        }

        /// <summary>
        /// 指定したアイテムを選択し、右側に詳細を表示する
        /// </summary>
        public void SelectItem(ItemData item)
        {
            _currentSelectedItem = item;

            // ボタンのハイライト更新
            foreach (var btn in _spawnedButtons)
            {
                if (btn != null)
                {
                    btn.SetSelected(btn.ItemData == item);
                }
            }

            ShowDetail(item);
        }

        /// <summary>
        /// 右側パネルにアイテムの文章を表示する
        /// </summary>
        private void ShowDetail(ItemData item)
        {
            if (item == null)
            {
                if (detailPanel != null) detailPanel.SetActive(false);
                if (emptyDetailMessage != null) emptyDetailMessage.SetActive(true);
                return;
            }

            if (detailPanel != null) detailPanel.SetActive(true);
            if (emptyDetailMessage != null) emptyDetailMessage.SetActive(false);

            // タイトル
            if (detailTitleText != null)
            {
                detailTitleText.text = item.itemName;
            }

            // アイコン
            if (detailIconImage != null)
            {
                if (item.icon != null)
                {
                    detailIconImage.sprite = item.icon;
                    detailIconImage.enabled = true;
                }
                else
                {
                    detailIconImage.enabled = false;
                }
            }

            // 本文文章（libraryContent優先、空ならdescription）
            if (detailContentText != null)
            {
                string content = !string.IsNullOrEmpty(item.libraryContent) 
                    ? item.libraryContent 
                    : !string.IsNullOrEmpty(item.description) 
                        ? item.description 
                        : "（記録された本文はありません）";

                detailContentText.text = content;
            }
        }
    }
}