using Prism.Translation.App.SupplierOrderModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Prism.Translation.App
{
    public static class NullableModule
    {
        public static T1? Map<T, T1>(this T? @this, Func<T, T1> f)
            where T : struct
            where T1 : struct
            => @this.HasValue ? f(@this.Value) : (T1?)null;
    }

    public static class ObjectModule
    {
        public static T1? Map<T, T1>(this T @this, Func<T, T1> f)
        {
            if (@this == null)
            {
                return default(T1);
            }
            else
            {
                return f(@this);
            }
        }
    }

    //<TranslationRequirement>
    //    <PressSheet externalRef = "12345" title="hello" shortDescription="this is a short description" longDescription="this is a long description">
    //        <orders>
    //            <order language = "spa" dueDate="2024/01/20T12:00:00Z" externalRef="1"/>
    //            <order language = "deu" dueDate="2024/01/20T12:00:00Z" externalRef="2"/>
    //            <order language = "ita" dueDate="2024/01/20T12:00:00Z" externalRef="3"/>
    //            <order language = "heb" dueDate="2024/01/20T13:00:00Z" externalRef="4"/>
    //        </orders>
    //    </PressSheet>
    //    <PressSheet externalRef = "12346" title="hello2" shortDescription="this is a short description2" longDescription="this is a long description2">
    //        <orders>
    //            <order language = "nor" dueDate="2024/02/01T12:00:00Z" externalRef="5"/>
    //            <order language = "swe" dueDate="2024/01/10T13:00:00Z" externalRef="6"/>
    //        </orders>
    //    </PressSheet>
    //</TranslationRequirement>
    namespace TranslationRequirement
    {
        public record Order(string language, DateTime dueDate, string externalRef);
        public record PressSheet(string externalRef, string title, string shortDescription, string language, IEnumerable<Order> orders);
        public record TranslationRequirement(IEnumerable<PressSheet> pressSheet);

        public static class TranslationRequirementModule
        {
            public static TranslationRequirement Load(string filename)
            {
                var xml = XDocument.Load(filename);
                return
                    new TranslationRequirement(
                from ps in xml.Root.Elements("PressSheet")
                let externalRef = ps.Attribute("externalRef").Value
                let title = ps.Attribute("title").Value
                let shortDescription = ps.Attribute("shortDescription").Value
                let language = ps.Attribute("language").Value
                select new PressSheet(
                            externalRef,
                            title,
                            shortDescription,
                            language,
                            from order in ps.Elements("orders").Elements("order")
                            select new Order(
                                order.Attribute("language").Value,
                                DateTime.Parse(order.Attribute("dueDate").Value),
                                order.Attribute("externalRef").Value))
                        );
            }
        }
    }

    namespace SupplierOrderModule
    {
        public record PressSheet(string language, string title, string shortDescription);
        public record Translation(PressSheet sourcePressSheet, string targetLanguage);
        public record Supplier(string name, string folder);
        public class Order
        {
            public Translation translation { get; init; }
            public Supplier? supplier { get; set; }

            public Order(Translation translation, Supplier? supplier)
            {
                this.translation = translation;
                this.supplier = supplier;
            }
        }
        public static class OrderModule
        {
            public static (Supplier,XDocument)? ToXml(Order order)
            {

                var doc = 
                    new XDocument(
                        new XElement("order",
                            new XElement("translation",
                                new XAttribute("sourceLanguage", order.translation.sourcePressSheet.language),
                                new XAttribute("targetLanguage", order.translation.targetLanguage),
                                new XAttribute("title", order.translation.sourcePressSheet.title),
                                new XAttribute("shortDescription", order.translation.sourcePressSheet.shortDescription)))                        
                        );
                return order.supplier.Map(supplier => 
                    (supplier,doc));
            }

            public static IEnumerable<Order> ToSupplierOrders(TranslationRequirement.TranslationRequirement tr)
            {
                return
                    from ps in tr.pressSheet
                    from order in ps.orders
                    let orderPs = new PressSheet(ps.language,ps.title,ps.shortDescription)
                    let translation = new Translation(orderPs, order.language)
                    select new Order(translation,null);
            }
        }
    }
}
