using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static XAML解析.MainWindow;
using System.Collections;

namespace XAML解析
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Type 类型 = typeof(List<List<string>>);
            //var 序列化 = 类型.序列化类型();
            //var 实例化 = Xaml序列化.实例化类型(序列化);
            //var 实例 = Xaml序列化.创建实例(类型);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var 解析xaml = Xaml解析器.解析字符串(源字符串控件.Text);
            解析字符串控件.Text = 解析xaml!.序列化();
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var 测试对象A = new 测试节点();
            {
                测试对象A.哈希表 = [new 测试节点(), new 测试节点()];
                for (int i = 0; i < 2; i++)
                {
                    var 子级A = new 测试节点 { ID = i, 姓名 = "B" + i };
                    for (int j = 0; j < 2; j++)
                    {
                        子级A.子级.Add(new 测试节点 { ID = j, 姓名 = "c" + j });
                    }
                    测试对象A.子级.Add(子级A);
                }
                for (int i = 0; i < 2; i++)
                {
                    测试对象A.哈希字典.Add(i, new 测试节点 { ID = i, 姓名 = "数据表" + i });
                }
                测试对象A.数组 = [0, 1, 3, 4, 8];
            }
            if (Xaml序列化.创建副本(测试对象A.哈希表) is HashSet<测试节点> 哈希表)
            {

            }
            if (Xaml序列化.创建副本(测试对象A.哈希字典) is Dictionary<int, 测试节点> 哈希字典)
            {

            }
            if (Xaml序列化.创建副本(测试对象A.泛型测试) is 测试泛型<int> 泛型测试)
            {

            }
            if (Xaml序列化.创建副本(测试对象A) is 测试节点 副本)
            {
                var XAML序列化A = Xaml序列化.序列化(测试对象A);
                var XAML序列化B = Xaml序列化.序列化(副本);
                源字符串控件.Text = XAML序列化A;
                解析字符串控件.Text = XAML序列化B;

                if (XAML序列化A == XAML序列化B)
                {

                }
            }

            List<循环引用测试> 集合A = [];
            List<循环引用测试> 集合B = [];
            for (int i = 0; i < 5; i++)
            {
                集合A.Add(new 循环引用测试($"A组[{i}]"));
                集合B.Add(new 循环引用测试($"B组[{i}]"));
            }
            for (int i = 0; i < 5; i++)
            {
                集合A[i].引用 = 集合B[i];
                集合B[i].引用 = 集合A[i < 4 ? i + 1 : 0];
            }
            集合A[0].引用集合 = 集合B;
            集合B[0].引用集合 = 集合A;
            if (Xaml序列化.创建副本(集合A) is List<循环引用测试> 引用副本)
            {

            }
        }
    }
    public class 测试泛型<T>
    {
        public T? 属性 { get; set; }
    }
    public class 测试节点
    {
        public int ID { get; set; } = 0;
        public string 姓名 { get; set; } = "A";

        public 测试泛型<int> 泛型测试 { get; set; } = new 测试泛型<int> { 属性 = 666 };
        public List<测试节点> 子级 { get; set; } = [];
        public int[] 数组 { get; set; } = [];
        public int[][] 多层数组 { get; set; } = new int[2][];

        public Dictionary<int, 测试节点> 哈希字典 { get; set; } = [];
        public HashSet<测试节点> 哈希表 { get; set; } = [];

        public 测试节点? 循环节点 { get; set; }

        public 测试节点()
        {
            多层数组 = [[1, 2], [3, 4]];

        }
    }
    public class 循环引用测试
    {
        public 循环引用测试()
        {

        }
        public 循环引用测试(string 名称)
        {
            this.名称 = 名称;
        }
        public string 名称 { get; set; } = "";
        public 循环引用测试? 引用 { get; set; }

        public List<循环引用测试>? 引用集合 { get; set; } = [];
    }

}