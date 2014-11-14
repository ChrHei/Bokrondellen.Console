using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.BusinessFoundation.Data.Meta;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Search;
using Mediachase.Search.Extensions;

namespace Bokrondellen.Console.Business.StockStatus
{
    public class StockStatusItemCollection : Dictionary<int, StockStatusItem>
    {
        public void Add(MetaEnumItem Item)
        {
            base.Add(Item.Handle, new StockStatusItem { MetaEnumItem = Item, Flag = 1 << Item.Handle - 1 });
        }

        public void AddRange(IEnumerable<MetaEnumItem> Items)
        {
            foreach (MetaEnumItem item in Items)
                this.Add(item);
        }

        public int GetFlag(IEnumerable<EntityObject> distInfos)
        {
            int flag = 0;

            IEnumerable<StockStatusItem> stockStatuses = distInfos.Select(di => this[(int)di["StockStatus"]]);

            foreach (StockStatusItem item in stockStatuses)
            {
                flag = flag | item.Flag;
            }

            return flag;
        }

        public int GetFlag(IEnumerable<int> stockStatusIds)
        {
            int flag = 0;

            IEnumerable<StockStatusItem> stockStatuses = stockStatusIds.Select(i => this[i]);

            foreach (StockStatusItem item in stockStatuses)
            {
                flag = flag | item.Flag;
            }

            return flag;
        }

        public string[] GetNames(IEnumerable<EntityObject> distInfos)
        {
            List<string> titles = new List<string>();

            IEnumerable<StockStatusItem> stockStatuses = distInfos.Select(di => this[(int)di["StockStatus"]]);

            return stockStatuses.Select(s => s.MetaEnumItem.Name).ToArray();
        }

        public void Load()
        {
            MetaFieldType stockStatusType = DataContext.Current.MetaModel.RegisteredTypes["StockStatusType"];

            AddRange(stockStatusType.EnumItems);
        }

        public IEnumerable<ISearchFacet> GetStockStatusFacets(ISearchFacetGroup FacetGroup)
        {
            List<ISearchFacet> facets = new List<ISearchFacet>();

            foreach (StockStatusItem item in this.Values)
            {
                int sum = 0;
                foreach (ISearchFacet facet in FacetGroup.Facets)
                {
                    int facetFlag = Convert.ToInt32(facet.Key, 2);
                    if ((facetFlag & item.Flag) == item.Flag)
                        sum += facet.Count;
                }
                if (sum > 0)
                    facets.Add(new Facet(FacetGroup, item.Flag.ToString(), item.MetaEnumItem.Name, sum));
            }

            FacetGroup.Facets.Clear();

            facets.ForEach(delegate(ISearchFacet facet)
            {
                FacetGroup.Facets.Add(facet);
            });

            return FacetGroup.Facets;
        }
    }
}
