using UnityEngine;
using System;
using System.Collections.Generic;
using Dracul.Item;

namespace Dracul.Player
{
    /// <summary>
    /// インベントリの1マス（スロット）を表すデータクラス。
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        [Tooltip("このスロットに入っているアイテムデータ（空の場合はnull）")]
        public ItemData itemData;

        [Tooltip("このスロットに入っている個数")]
        public int count;

        /// <summary>スロットが空かどうか</summary>
        public bool IsEmpty => itemData == null || count <= 0;

        /// <summary>スタック上限に達しているか</summary>
        public bool IsFull => itemData != null && count >= itemData.maxStackCount;

        /// <summary>まだ追加できる数</summary>
        public int RemainingCapacity => itemData != null ? itemData.maxStackCount - count : 0;

        /// <summary>スロットをクリアする</summary>
        public void Clear()
        {
            itemData = null;
            count = 0;
        }
    }

    /// <summary>
    /// プレイヤーのアイテム所持をスロット方式で管理する。
    /// 各スロットにはアイテムデータと個数が入り、スタック上限に達した場合は
    /// 新しい空きスロットに溢れた分が格納される。
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory")]
        [Tooltip("インベントリのスロット数（4列×2行 = 8）")]
        public int maxSlots = 8;

        [Tooltip("現在のインベントリスロット一覧（インスペクターで確認可）")]
        [SerializeField]
        private List<InventorySlot> _slots = new List<InventorySlot>();

        /// <summary>スロットリストへの読み取り専用アクセス</summary>
        public IReadOnlyList<InventorySlot> Slots => _slots;

        /// <summary>インベントリの内容が変更された際に発火するイベント</summary>
        public event Action OnInventoryChanged;

        void Awake()
        {
            // スロットが足りない場合は初期化する
            while (_slots.Count < maxSlots)
            {
                _slots.Add(new InventorySlot());
            }
        }

        /// <summary>
        /// アイテムをインベントリに追加する。
        /// スタック可能なスロットを優先し、上限に達していれば新しい空きスロットに格納する。
        /// </summary>
        /// <param name="data">追加するアイテムデータ</param>
        /// <param name="amount">追加する数（省略時は ItemData.pickupAmount を使用）</param>
        /// <returns>実際に追加できた数。0なら追加不可（インベントリ満杯）</returns>
        public int AddItem(ItemData data, int amount = -1)
        {
            if (data == null) return 0;

            if (amount < 0) amount = data.pickupAmount;

            int remaining = amount;
            int totalAdded = 0;

            // ステップ1: すでに同じアイテムが入っていて、まだスタックに余裕があるスロットを探す
            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = _slots[i];
                if (slot.itemData == data && !slot.IsFull)
                {
                    int canAdd = Mathf.Min(remaining, slot.RemainingCapacity);
                    slot.count += canAdd;
                    remaining -= canAdd;
                    totalAdded += canAdd;
                }
            }

            // ステップ2: まだ残りがある場合は、空きスロットに新しく入れる
            while (remaining > 0)
            {
                int emptyIndex = FindEmptySlot();
                if (emptyIndex < 0)
                {
                    // 空きスロットがないので、これ以上追加できない
                    break;
                }

                int canAdd = Mathf.Min(remaining, data.maxStackCount);
                _slots[emptyIndex].itemData = data;
                _slots[emptyIndex].count = canAdd;
                remaining -= canAdd;
                totalAdded += canAdd;
            }

            if (totalAdded > 0)
            {
                Debug.Log($"[Inventory] 「{data.itemName}」を {totalAdded} 個取得！");
                OnInventoryChanged?.Invoke();
            }
            else
            {
                Debug.Log($"[Inventory] インベントリが満杯のため「{data.itemName}」を取得できませんでした。");
            }

            return totalAdded;
        }

        /// <summary>
        /// 指定したスロットのアイテムを1つ消費する（将来の使用処理用）。
        /// </summary>
        /// <param name="slotIndex">消費するスロットのインデックス</param>
        /// <returns>消費に成功した場合 true</returns>
        public bool ConsumeFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return false;

            InventorySlot slot = _slots[slotIndex];
            if (slot.IsEmpty) return false;

            slot.count--;
            if (slot.count <= 0)
            {
                slot.Clear();
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// アイテムをインベントリから削除する（将来の使用・消費処理用）。
        /// 指定したアイテムを持っているスロットから1つ減らす。
        /// </summary>
        /// <param name="data">削除するアイテムデータ</param>
        /// <returns>削除に成功した場合 true</returns>
        public bool RemoveItem(ItemData data)
        {
            if (data == null) return false;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].itemData == data && _slots[i].count > 0)
                {
                    _slots[i].count--;
                    if (_slots[i].count <= 0)
                    {
                        _slots[i].Clear();
                    }
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定アイテムの合計所持数を取得する。
        /// </summary>
        public int GetItemCount(ItemData data)
        {
            if (data == null) return 0;
            int total = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].itemData == data)
                {
                    total += _slots[i].count;
                }
            }
            return total;
        }

        /// <summary>
        /// アイテムを追加できるかどうかを確認する（追加はしない）。
        /// </summary>
        public bool CanAddItem(ItemData data, int amount = -1)
        {
            if (data == null) return false;
            if (amount < 0) amount = data.pickupAmount;

            int remaining = amount;

            // スタック可能なスロットの空き容量を計算
            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                if (_slots[i].itemData == data && !_slots[i].IsFull)
                {
                    remaining -= _slots[i].RemainingCapacity;
                }
            }

            // 空きスロットの容量を計算
            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    remaining -= data.maxStackCount;
                }
            }

            return remaining <= 0;
        }

        /// <summary>空きスロットのインデックスを返す。なければ -1</summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
