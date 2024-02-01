using Kookerella.FileScanner;
using Prism.Translation.App;
using Prism.Translation.App.SupplierOrderModule;
using Prism.Translation.App.TranslationRequirement;
using System.Xml.Linq;

namespace Prism.Subtitle.App
{
    public partial class Form1 : Form
    {
        Translation.App.SupplierOrderModule.Order[] orders = new Translation.App.SupplierOrderModule.Order[] { };

        public Form1()
        {
            InitializeComponent();
        }

        void HandleFile(FileInfo fi)
        {
            if (this.InvokeRequired)
            {
                Invoke(() => this.HandleFile(fi));
            }
            else
            {
                var tr = TranslationRequirementModule.Load(fi.FullName);
                this.orders = OrderModule.ToSupplierOrders(tr).ToArray();

                // do all the hard stuff in a grid!


                MessageBox.Show("orders generated " + fi.FullName);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var scanner = SimpleFolderScanner.NewSimpleFolderScanner(
                "C:\\temp\\translationrequest",
                "*.xml",
                HandleFile,
                (e) => { });
        }
        public static IEnumerable<T> RepeatIndefinitely<T>(IEnumerable<T> source)
        {
            while (true)
            {
                foreach (var item in source)
                {
                    yield return item;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var suppliers =
                new Supplier[]
                {
                    new Supplier("supplier1", "c:\\temp\\supplier1"),
                    new Supplier("supplier2", "c:\\temp\\supplier2"),
                    new Supplier("supplier3", "c:\\temp\\supplier3"),
                };

            var suppliersRepeat = RepeatIndefinitely(suppliers);
            var enumerator = suppliersRepeat.GetEnumerator();

            foreach (var order in this.orders)
            {
                enumerator.MoveNext();
                order.supplier = enumerator.Current;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var sideEffects = 
                from order in this.orders
                where order != null
                select OrderModule.ToXml(order).GetValueOrDefault();

            foreach (var sideEffect in sideEffects)
            {
                var now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
                sideEffect.Item2.Save(Path.Combine(sideEffect.Item1.folder, $"""{now}.xml"""));
            }
        }
    }
}