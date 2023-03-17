using SB_Parser_API.Models;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;
using static System.Web.HttpUtility;
using static SB_Parser_API.MicroServices.WebAccessUtils;
using static SB_Parser_API.MicroServices.DBSerices;
using static SB_Parser_API.MicroServices.Utils;
using static SB_Parser_API.MicroServices.SB_API;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using AutoMapper;
//using AutoMapperApp.Models;

namespace SB_Parser_API.MicroServices
{
    public class ZC_API
    {
        public static Products_List_From_Barcode_ZC productInfoListFromBarcode(string barcode, ZC_User user, long query_id)
        {
            var orbar = barcode;
            barcode = BarCodeCheck(barcode);

            string error="";
            if (barcode.Length <= 0) error = $"{{Error:'Invalid barcode -> [{orbar}]'}}"; 
            var bidl = GetBarcodeIdList(barcode);
            if (error.Length <= 0 && bidl.Count() <= 0) error = $"{{Error:'Product not found (barcode:[{orbar}])'}}";
            if (error.Length > 0)
                return new Products_List_From_Barcode_ZC(query_id, user.id, new(), error);

            if (user.id <= 0)
                AddNewUser(user);
            if (query_id <= 0)
            {
                var query = new Query_ZC() { id = query_id, user_id = user.id, query = barcode, query_type = QueryType.BarcodeSearch, dt = DateTime.Now };
                AddNewQuery(query);
                query_id = query.id;
            }

            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Product_SB_V2, Product_From_Barcode_ZC>().ReverseMap();
            }).CreateMapper();

            Products_List_From_Barcode_ZC productList = new(query_id, user.id, new List<Product_From_Barcode_ZC>(), "");

            foreach (var bid in bidl)
            {
                var prod = mapper.Map<Product_From_Barcode_ZC>(GetProductFromBarcodeId((int)bid));
                prod.barcode = barcode;
                productList.products.Add(productInfoFromProductID(prod));
            }
            return productList;
        }
        public static Product_From_Barcode_ZC productInfoFromProductID(Product_From_Barcode_ZC product)
        {
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Price_SB_V2, Price_ZC>().ReverseMap();
                cfg.CreateMap<Property_Product_SB_V2, Property_Product_DB>().ReverseMap();
            }).CreateMapper();

            var pricesSBV2 = GetPricesFromSKU(product.sku ?? 0);
            List<Price_ZC> prices = new();
            pricesSBV2?.ForEach(x => prices.Add(mapper.Map<Price_ZC>(x)));
            prices.ForEach(x => { var ret = GetRetailer(x.retailer ?? 0); x.retailer_name = ret?.name; x.retailer_logo_url = ret?.logo_image; x.retailer_mini_logo_url = ret.mini_logo_image; });
            product.prices = prices;
            product.image_urls = GetImagesFromSKU(product.sku ?? 0);
            //JsonConvert.SerializeObject
            var propDB_List = GetProductPropertiesFromSKU(product.sku ?? 0) ?? new();
            List<Property_Product_SB_V2> properties;
            if ((propDB_List?.Count ?? 0) <= 0)
            {
                var productInfo = productInfoGet(product.id);
                properties = productInfo?.properties ?? new();
                var mtaxon = JsonConvert.SerializeObject(productInfo?.main_taxon).Replace("\"", "'");
                properties.Add(new Property_Product_SB_V2() { name = "main_taxon", presentation = "Категория", value = mtaxon });
                properties.Add(new Property_Product_SB_V2() { name = "brand", presentation = "Brand", value = productInfo?.brand_name });
                properties.Add(new Property_Product_SB_V2() { name = "brand_id", presentation = "Brand_ID", value = productInfo?.brand_id.ToString() });
                properties.Add(new Property_Product_SB_V2() { name = "description", presentation = "Описание товара", value = productInfo?.description });
                propDB_List = new();
                properties.ForEach((x => propDB_List.Add(mapper.Map<Property_Product_DB>(x))));
                propDB_List.ForEach(x => { x.product_id = product.id; x.product_sku = product.sku ?? 0; });
                Task.Factory.StartNew(() => AddProductProperties(propDB_List), TaskCreationOptions.LongRunning);
            }
            else
            {
                properties = new();
                propDB_List?.ForEach(x => properties.Add(mapper.Map<Property_Product_SB_V2>(x)));
            }
            product.properties = properties;
            return product;
        }
    }
}
