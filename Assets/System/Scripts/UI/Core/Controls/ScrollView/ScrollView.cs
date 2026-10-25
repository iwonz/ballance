using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SObject = System.Object;
using UnityEngine.Pool;

namespace Ballance2.UI.Core.Controls
{
  [RequireComponent(typeof(RectTransform))]
  [DisallowMultipleComponent]
  public class ScrollView : ScrollRect
  {
    private class ScrollItemWithRect
    {
      // Scroll item's RectTransform.
      public RectTransform item;

      // Scroll item position within the scroll view.
      public Rect rect;

      // Whether the rect needs updating.
      public bool rectDirty = true;
    }

    int m_dataCount = 0;
    List<ScrollItemWithRect> managedItems = new List<ScrollItemWithRect>();

    // for hide and show
    public enum ItemLayoutType
    {
      // The last bit encodes scrolling direction.
      Vertical = 1,                   // 0001
      Horizontal = 2,                 // 0010
      VerticalThenHorizontal = 4,     // 0100
      HorizontalThenVertical = 5,     // 0101
    }
    
    public const int flagScrollDirection = 1;  // 0001

    [SerializeField]
    ItemLayoutType m_layoutType = ItemLayoutType.Vertical;
    protected ItemLayoutType layoutType { get { return m_layoutType; } }

    // Integer constants avoid repeated enum casts.
    protected static class CriticalItemType
    {
      public const int UpToHide = 0;
      public const int DownToHide = 1;
      public const int UpToShow = 2;
      public const int DownToShow = 3;
    }
    // Keep only four boundary indices.
    protected int[] criticalItemIndex = new int[4];
    Rect refRect;

    // resource management
    ObjectPool<RectTransform> itemPool = null;

    [Tooltip("Initial number of pooled items.")]
    public int poolSize;
    [Tooltip("Default item size.")]
    public Vector2 defaultItemSize;
    [Tooltip("Item template.")]
    public RectTransform itemTemplate;
    public delegate void UpdateFunc(int index, RectTransform item);
    public delegate Vector2 ItemSizeFunc(int index);
    public delegate int ItemCountFunc();
    public delegate RectTransform ItemGetFunc(int index);
    public delegate void ItemRecycleFunc(RectTransform item);

    // callbacks for items
    
    public UpdateFunc updateFunc;
    public ItemSizeFunc itemSizeFunc;
    public ItemCountFunc itemCountFunc;
    public ItemGetFunc itemGetFunc;
    public ItemRecycleFunc itemRecycleFunc;

    // status
    private bool initialized = false;
    private int willUpdateData = 0;

    
    public virtual void SetUpdateFunc(UpdateFunc func)
    {
      updateFunc = func;
    }
    
    public virtual void SetItemSizeFunc(ItemSizeFunc func)
    {
      itemSizeFunc = func;
    }
    
    public virtual void SetItemCountFunc(ItemCountFunc func)
    {
      itemCountFunc = func;
    }
    
    public void SetItemGetAndRecycleFunc(ItemGetFunc getFunc, ItemRecycleFunc recycleFunc)
    {
      if (getFunc != null && recycleFunc != null)
      {
        itemGetFunc = getFunc;
        itemRecycleFunc = recycleFunc;
      }
    }
    
    public void UpdateData(bool immediately = true)
    {
      if (!initialized)
      {
        InitScrollView();
      }
      if (immediately)
      {
        willUpdateData |= 3; // 0011
        InternalUpdateData();
      }
      else
      {
        if (willUpdateData == 0)
        {
          StartCoroutine(DelayUpdateData());
        }
        willUpdateData |= 3;
      }
    }
    
    public void UpdateDataIncrementally(bool immediately = true)
    {
      if (!initialized)
      {
        InitScrollView();
      }
      if (immediately)
      {
        willUpdateData |= 1; // 0001
        InternalUpdateData();
      }
      else
      {
        if (willUpdateData == 0)
        {
          StartCoroutine(DelayUpdateData());
        }
        willUpdateData |= 1;
      }
    }

    public void ScrollTo(int index)
    {
      InternalScrollTo(index);
    }

    protected virtual void InternalScrollTo(int index)
    {
      index = Mathf.Clamp(index, 0, m_dataCount - 1);
      EnsureItemRect(index);
      Rect r = managedItems[index].rect;
      int dir = (int)layoutType & flagScrollDirection;
      if (dir == 1)
      {
        // vertical
        float value = 1 - (-r.yMax / (content.sizeDelta.y - refRect.height));
        //value = Mathf.Clamp01(value);
        SetNormalizedPosition(value, 1);
      }
      else
      {
        // horizontal
        float value = r.xMin / (content.sizeDelta.x - refRect.width);
        //value = Mathf.Clamp01(value);
        SetNormalizedPosition(value, 0);
      }
    }

    private IEnumerator DelayUpdateData()
    {
      yield return null;
      InternalUpdateData();
    }

    private void InternalUpdateData()
    {
      int newDataCount = 0;
      bool keepOldItems = ((willUpdateData & 2) == 0);

      if (itemCountFunc != null)
      {
        newDataCount = itemCountFunc();
      }

      if (newDataCount != managedItems.Count)
      {
        if (managedItems.Count < newDataCount) // Increment.
        {
          if (!keepOldItems)
          {
            foreach (var itemWithRect in managedItems)
            {
              // Reset all rects.
              itemWithRect.rectDirty = true;
            }
          }

          while (managedItems.Count < newDataCount)
          {
            managedItems.Add(new ScrollItemWithRect());
          }
        }
        else // Keep spare capacity to avoid garbage collection.
        {
          for (int i = 0, count = managedItems.Count; i < count; ++i)
          {
            if (i < newDataCount)
            {
              // Reset all rects.
              if (!keepOldItems)
              {
                managedItems[i].rectDirty = true;
              }

              if (i == newDataCount - 1)
              {
                managedItems[i].rectDirty = true;
              }
            }

            // Recycle items outside the required range.
            if (i >= newDataCount)
            {
              managedItems[i].rectDirty = true;
              if (managedItems[i].item != null)
              {
                RecycleOldItem(managedItems[i].item);
                managedItems[i].item = null;
              }
            }
          }
        }
      }
      else
      {
        if (!keepOldItems)
        {
          for (int i = 0, count = managedItems.Count; i < count; ++i)
          {
            // Reset all rects.
            managedItems[i].rectDirty = true;
          }
        }
      }

      m_dataCount = newDataCount;

      ResetCriticalItems();

      willUpdateData = 0;
    }

    void ResetCriticalItems()
    {
      bool hasItem, shouldShow;
      int firstIndex = -1, lastIndex = -1;

      for (int i = 0; i < m_dataCount; i++)
      {
        hasItem = managedItems[i].item != null;
        shouldShow = ShouldItemSeenAtIndex(i);

        if (shouldShow)
        {
          if (firstIndex == -1)
          {
            firstIndex = i;
          }
          lastIndex = i;
        }

        if (hasItem && shouldShow)
        {
          // Should be visible and already is.
          SetDataForItemAtIndex(managedItems[i].item, i);
          continue;
        }

        if (hasItem == shouldShow)
        {
          // Should be hidden and already is.
          //if (firstIndex != -1)
          //{
          //     // All visible items have been visited; skip the rest for now.
          //    break;
          //}
          continue;
        }

        if (hasItem && !shouldShow)
        {
          // Should be hidden but exists.
          RecycleOldItem(managedItems[i].item);
          managedItems[i].item = null;
          continue;
        }

        if (shouldShow && !hasItem)
        {
          // Should be visible but is missing.
          RectTransform item = GetNewItem(i);
          OnGetItemForDataIndex(item, i);
          managedItems[i].item = item;
          continue;
        }

      }

      // content.localPosition = Vector2.zero;
      criticalItemIndex[CriticalItemType.UpToHide] = firstIndex;
      criticalItemIndex[CriticalItemType.DownToHide] = lastIndex;
      criticalItemIndex[CriticalItemType.UpToShow] = Mathf.Max(firstIndex - 1, 0);
      criticalItemIndex[CriticalItemType.DownToShow] = Mathf.Min(lastIndex + 1, m_dataCount - 1);

    }

    protected override void SetContentAnchoredPosition(Vector2 position)
    {
      base.SetContentAnchoredPosition(position);
      UpdateCriticalItems();
    }

    protected override void SetNormalizedPosition(float value, int axis)
    {
      base.SetNormalizedPosition(value, axis);
      ResetCriticalItems();
    }

    RectTransform GetCriticalItem(int type)
    {
      int index = criticalItemIndex[type];
      if (index >= 0 && index < m_dataCount)
      {
        return managedItems[index].item;
      }
      return null;
    }

    void UpdateCriticalItems()
    {
      bool dirty = true;

      while (dirty)
      {
        dirty = false;

        for (int i = CriticalItemType.UpToHide; i <= CriticalItemType.DownToShow; i++)
        {
          if (i <= CriticalItemType.DownToHide) // Hide items leaving the visible area.
          {
            dirty = dirty || CheckAndHideItem(i);
          }
          else  // Show items entering the visible area.
          {
            dirty = dirty || CheckAndShowItem(i);
          }
        }
      }
    }

    private bool CheckAndHideItem(int criticalItemType)
    {
      RectTransform item = GetCriticalItem(criticalItemType);
      int criticalIndex = criticalItemIndex[criticalItemType];
      if (item != null && !ShouldItemSeenAtIndex(criticalIndex))
      {
        RecycleOldItem(item);
        managedItems[criticalIndex].item = null;
        // Debug.Log("Recycled " + criticalIndex);

        if (criticalItemType == CriticalItemType.UpToHide)
        {
          // One item hidden at the top.
          criticalItemIndex[criticalItemType + 2] = Mathf.Max(criticalIndex, criticalItemIndex[criticalItemType + 2]);
          criticalItemIndex[criticalItemType]++;
        }
        else
        {
          // One item hidden at the bottom.
          criticalItemIndex[criticalItemType + 2] = Mathf.Min(criticalIndex, criticalItemIndex[criticalItemType + 2]);
          criticalItemIndex[criticalItemType]--;
        }
        criticalItemIndex[criticalItemType] = Mathf.Clamp(criticalItemIndex[criticalItemType], 0, m_dataCount - 1);
        return true;
      }

      return false;
    }

    private bool CheckAndShowItem(int criticalItemType)
    {
      RectTransform item = GetCriticalItem(criticalItemType);
      int criticalIndex = criticalItemIndex[criticalItemType];
      //if (item == null && ShouldItemFullySeenAtIndex(criticalItemIndex[criticalItemType - 2]))

      if (item == null && ShouldItemSeenAtIndex(criticalIndex))
      {
        RectTransform newItem = GetNewItem(criticalIndex);
        OnGetItemForDataIndex(newItem, criticalIndex);
        // Debug.Log("Created " + criticalIndex);
        managedItems[criticalIndex].item = newItem;


        if (criticalItemType == CriticalItemType.UpToShow)
        {
          // One item shown at the top.
          criticalItemIndex[criticalItemType - 2] = Mathf.Min(criticalIndex, criticalItemIndex[criticalItemType - 2]);
          criticalItemIndex[criticalItemType]--;
        }
        else
        {
          // One item shown at the bottom.
          criticalItemIndex[criticalItemType - 2] = Mathf.Max(criticalIndex, criticalItemIndex[criticalItemType - 2]);
          criticalItemIndex[criticalItemType]++;
        }
        criticalItemIndex[criticalItemType] = Mathf.Clamp(criticalItemIndex[criticalItemType], 0, m_dataCount - 1);
        return true;
      }
      return false;
    }

    bool ShouldItemSeenAtIndex(int index)
    {
      if (index < 0 || index >= m_dataCount)
      {
        return false;
      }
      EnsureItemRect(index);
      return new Rect(refRect.position - content.anchoredPosition, refRect.size).Overlaps(managedItems[index].rect);
    }

    bool ShouldItemFullySeenAtIndex(int index)
    {
      if (index < 0 || index >= m_dataCount)
      {
        return false;
      }
      EnsureItemRect(index);
      return IsRectContains(new Rect(refRect.position - content.anchoredPosition, refRect.size), (managedItems[index].rect));
    }

    bool IsRectContains(Rect outRect, Rect inRect, bool bothDimensions = false)
    {

      if (bothDimensions)
      {
        bool xContains = (outRect.xMax >= inRect.xMax) && (outRect.xMin <= inRect.xMin);
        bool yContains = (outRect.yMax >= inRect.yMax) && (outRect.yMin <= inRect.yMin);
        return xContains && yContains;
      }
      else
      {
        int dir = (int)layoutType & flagScrollDirection;
        if (dir == 1)
        {
          // Vertical scrolling uses only Y.
          return (outRect.yMax >= inRect.yMax) && (outRect.yMin <= inRect.yMin);
        }
        else // = 0
        {
          // Horizontal scrolling uses only X.
          return (outRect.xMax >= inRect.xMax) && (outRect.xMin <= inRect.xMin);
        }
      }
    }

    void InitPool()
    {
      GameObject poolNode = new GameObject("POOL");
      poolNode.SetActive(false);
      poolNode.transform.SetParent(transform, false);
      itemPool = new ObjectPool<RectTransform>(
          createFunc: () =>
          {
            GameObject itemObj = Instantiate(itemTemplate.gameObject);
            RectTransform item = itemObj.GetComponent<RectTransform>();
            itemObj.transform.SetParent(poolNode.transform, false);

            item.anchorMin = Vector2.up;
            item.anchorMax = Vector2.up;
            item.pivot = Vector2.zero;
                  //rectTrans.pivot = Vector2.up;

                  itemObj.SetActive(true);
            return item;
          },
          actionOnRelease: item => item.SetParent(poolNode.transform, false),
          actionOnDestroy: item =>
          {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item.gameObject);
            else DestroyImmediate(item.gameObject);
          },
          defaultCapacity: Mathf.Max(0, poolSize), maxSize: Mathf.Max(1, poolSize));
    }

    void OnGetItemForDataIndex(RectTransform item, int index)
    {
      SetDataForItemAtIndex(item, index);
      item.transform.SetParent(content, false);
    }

    void SetDataForItemAtIndex(RectTransform item, int index)
    {
      if (updateFunc != null)
        updateFunc(index, item);

      SetPosForItemAtIndex(item, index);
    }

    void SetPosForItemAtIndex(RectTransform item, int index)
    {
      EnsureItemRect(index);
      Rect r = managedItems[index].rect;
      item.localPosition = r.position;
      item.sizeDelta = r.size;
    }

    Vector2 GetItemSize(int index)
    {
      if (index >= 0 && index <= m_dataCount)
      {
        if (itemSizeFunc != null)
        {
          return itemSizeFunc(index);
        }
      }
      return defaultItemSize;
    }

    private RectTransform GetNewItem(int index)
    {
      RectTransform item;
      if (itemGetFunc != null)
      {
        item = itemGetFunc(index);
      }
      else
      {
        item = itemPool.Get();
      }
      return item;
    }

    private void RecycleOldItem(RectTransform item)
    {
      if (itemRecycleFunc != null)
      {
        itemRecycleFunc(item);
      }
      else
      {
        itemPool.Release(item);
      }
    }

    void InitScrollView()
    {
      initialized = true;

      // Apply the configured direction to the underlying ScrollRect.
      int dir = (int)layoutType & flagScrollDirection;
      vertical = (dir == 1);
      horizontal = (dir == 0);

      content.pivot = Vector2.up;
      InitPool();
      UpdateRefRect();
    }


    Vector3[] viewWorldConers = new Vector3[4];
    Vector3[] rectCorners = new Vector3[2];
    void UpdateRefRect()
    {
      /*
       *  WorldCorners
       * 
       *    1 ------- 2     
       *    |         |
       *    |         |
       *    0 ------- 3
       * 
       */

      // refRect is the viewport rect relative to Content.
      viewRect.GetWorldCorners(viewWorldConers);
      rectCorners[0] = content.transform.InverseTransformPoint(viewWorldConers[0]);
      rectCorners[1] = content.transform.InverseTransformPoint(viewWorldConers[2]);
      refRect = new Rect((Vector2)rectCorners[0] - content.anchoredPosition, rectCorners[1] - rectCorners[0]);
    }

    void MovePos(ref Vector2 pos, Vector2 size)
    {
      // All rects use the bottom-left corner as their origin.
      switch (layoutType)
      {
        case ItemLayoutType.Vertical:
          // Move downward vertically.
          pos.y -= size.y;
          break;
        case ItemLayoutType.Horizontal:
          // Move right horizontally.
          pos.x += size.x;
          break;
        case ItemLayoutType.VerticalThenHorizontal:
          pos.y -= size.y;
          if (pos.y <= -refRect.height)
          {
            pos.y = 0;
            pos.x += size.x;
          }
          break;
        case ItemLayoutType.HorizontalThenVertical:
          pos.x += size.x;
          if (pos.x >= refRect.width)
          {
            pos.x = 0;
            pos.y -= size.y;
          }
          break;
        default:
          break;
      }
    }

    protected void EnsureItemRect(int index)
    {
      if (!managedItems[index].rectDirty)
      {
        // Already clean.
        return;
      }

      ScrollItemWithRect firstItem = managedItems[0];
      if (firstItem.rectDirty)
      {
        Vector2 firstSize = GetItemSize(0);
        firstItem.rect = CreateWithLeftTopAndSize(Vector2.zero, firstSize);
        firstItem.rectDirty = false;
      }

      // Nearest updated rect before the current item.
      int nearestClean = 0;
      for (int i = index; i >= 0; --i)
      {
        if (!managedItems[i].rectDirty)
        {
          nearestClean = i;
          break;
        }
      }

      // Update sizes from nearestClean through index.
      Rect nearestCleanRect = managedItems[nearestClean].rect;
      Vector2 curPos = GetLeftTop(nearestCleanRect);
      Vector2 size = nearestCleanRect.size;
      MovePos(ref curPos, size);

      for (int i = nearestClean + 1; i <= index; i++)
      {
        size = GetItemSize(i);
        managedItems[i].rect = CreateWithLeftTopAndSize(curPos, size);
        managedItems[i].rectDirty = false;
        MovePos(ref curPos, size);
      }
      Vector2 range = new Vector2(Mathf.Abs(curPos.x), Mathf.Abs(curPos.y));
      switch (layoutType)
      {
        case ItemLayoutType.VerticalThenHorizontal:
          range.x += size.x;
          range.y = refRect.height;
          break;
        case ItemLayoutType.HorizontalThenVertical:
          range.x = refRect.width;
          if (curPos.x != 0)
          {
            range.y += size.y;
          }
          break;
        default:
          break;
      }
      content.sizeDelta = range;
    }

    private static Vector2 GetLeftTop(Rect rect)
    {
      Vector2 ret = rect.position;
      ret.y += rect.size.y;
      return ret;
    }
    private static Rect CreateWithLeftTopAndSize(Vector2 leftTop, Vector2 size)
    {
      Vector2 leftBottom = leftTop - new Vector2(0, size.y);
      return new Rect(leftBottom, size);
    }


    protected override void OnDestroy()
    {
      if (itemPool != null)
      {
        itemPool.Dispose();
      }
      base.OnDestroy();
    }

    protected Rect GetItemLocalRect(int index)
    {
      if (index >= 0 && index < m_dataCount)
      {
        EnsureItemRect(index);
        return managedItems[index].rect;
      }
      return new Rect();
    }
  }

}
