using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XAML解析
{
    /// <summary>
    /// 这里负责序列化
    /// </summary>
    public static partial class Xaml序列化
    {
        static Dictionary<object, Xaml节点> 循环引用对象集合 = [];
        public static string 序列化(object 对象)
        {
            循环引用对象集合 = [];
            if (对象递归序列化Xaml(对象) is Xaml节点 xaml)
            {
                循环引用对象集合 = [];
                return xaml.ToString();
            }
            else
            {
                循环引用对象集合 = [];
                throw new Exception("错误");
            }
        }
        private static Xaml节点? 对象递归序列化Xaml(object 对象)
        {
            if (对象 == null) return null;
            var 对象类型 = 对象.GetType();
            if (检查是基础类型(对象类型))
            {
                return new Xaml节点(对象类型.序列化类型()) { 内容 = 对象.ToString()! };
            }
            if (循环引用对象集合.ContainsKey(对象))
            {
                if (!循环引用对象集合[对象].属性集合.ContainsKey("GUID"))
                    循环引用对象集合[对象].属性集合.Add("GUID", Guid.NewGuid().ToString());
                return new Xaml节点("GUID") { 内容 = 循环引用对象集合[对象].属性集合["GUID"] };
            }

            if (Attribute.IsDefined(对象类型, typeof(Xaml序列化忽略)))
            {
                return null;//循环引用
            }
            if (是否包含特性方法(对象类型, typeof(Xaml序列化类判断), out var Xaml序列化类判断方法, BindingFlags.Instance | BindingFlags.Public)) // 如果对象类包含判断是否序列化的方法
            {
                if (Xaml序列化类判断方法!.ReturnType == typeof(bool) || Xaml序列化类判断方法.GetParameters().Length == 0)// 判断函数是否合法
                {
                    if (!(bool)Xaml序列化类判断方法.Invoke(对象, [])!)
                    {
                        return null; //该实例不允许实例化
                    }
                }
            }
            if (是否包含特性方法(对象类型, typeof(Xaml序列化方法), out var 序列化方法, BindingFlags.Instance | BindingFlags.Public)) // 如果对象类包含序列化的方法
            {
                if (序列化方法!.ReturnType == typeof(Xaml节点) || 序列化方法.GetParameters().Length == 0) // 判断函数是否合法
                {
                    string 序列化字符串 = (string)序列化方法.Invoke(null, [对象])!;
                    var 序列化节点 = Xaml解析器.解析字符串(序列化字符串)!.根节点.子级集合[0];//使用该方法进行序列化
                    if (!循环引用对象集合.ContainsKey(对象)) 循环引用对象集合.Add(对象, 序列化节点);
                    return 序列化节点;
                }
            }
            if (对象 is IDictionary 哈希表)
            {
                var 字典节点 = new Xaml节点(对象类型.序列化类型());
                if (!循环引用对象集合.ContainsKey(对象)) 循环引用对象集合.Add(对象, 字典节点);

                Type[] 泛型元素类型 = 对象类型.GetGenericArguments();
                var 键节点 = new Xaml节点(泛型元素类型[0].序列化类型());
                foreach (var Key in 哈希表.Keys)
                {
                    if (对象递归序列化Xaml(Key) is Xaml节点 子级节点)
                        键节点.子级集合.Add(子级节点);
                }
                var 值节点 = new Xaml节点(泛型元素类型[1].序列化类型());
                foreach (var Value in 哈希表.Values)
                {
                    if (对象递归序列化Xaml(Value) is Xaml节点 子级节点)
                        值节点.子级集合.Add(子级节点);
                }
                字典节点.子级集合.Add(键节点);
                字典节点.子级集合.Add(值节点);
                return 字典节点;
            }
            else if (对象 is IEnumerable 遍历对象) // 对象是数组
            {
                var 数组节点 = new Xaml节点(对象类型.序列化类型());
                if (!循环引用对象集合.ContainsKey(对象)) 循环引用对象集合.Add(对象, 数组节点);

                foreach (var 元素 in 遍历对象)
                {
                    if (对象递归序列化Xaml(元素) is Xaml节点 子级节点)
                        数组节点.子级集合.Add(子级节点);
                }
                return 数组节点;
            }
            else  //开始序列化属性
            {
                Xaml节点 新节点 = new Xaml节点(对象类型.序列化类型()!);
                if (!循环引用对象集合.ContainsKey(对象)) 循环引用对象集合.Add(对象, 新节点);

                var 对象属性集合 = 对象类型.GetProperties(BindingFlags.Instance | BindingFlags.Public);//获取所有属性,但不包括静态属性
                HashSet<string>? 序列化指定属性 = null;
                if (是否包含特性方法(对象类型, typeof(Xaml序列化指定属性), out var Xaml序列化指定属性, BindingFlags.Static | BindingFlags.Public))
                    序列化指定属性 = (HashSet<string>?)Xaml序列化指定属性!.Invoke(null, []);
                Dictionary<string, object> 嵌套属性集合 = [];
                foreach (var 对象属性 in 对象属性集合)
                {
                    if (序列化指定属性 == null || 序列化指定属性.Contains(对象属性.Name))
                    {
                        if (!Attribute.IsDefined(对象属性, typeof(Xaml序列化忽略)) && !对象属性.PropertyType.IsSubclassOf(typeof(Delegate))) //排除忽略，排除委托参数
                            if (对象属性.CanRead && 对象属性.CanWrite && 对象属性.GetIndexParameters().Length == 0)//检查属性是否有get、set方法,并且排除索引器属性
                            {
                                var 属性值 = 对象属性.GetValue(对象);
                                if (属性值 == null) //检查值是否为空
                                {
                                    新节点.属性集合.Add(对象属性.Name, "null");
                                }
                                else if (对象属性.PropertyType.IsValueType || 对象属性.PropertyType == typeof(string) || 对象属性.PropertyType.IsEnum) //属性是值类型，包括结构体、枚举，要保证对象正确的实现了tostring的方法
                                {
                                    新节点.属性集合.Add(对象属性.Name, 属性值.ToString()!);
                                }
                                else
                                {
                                    嵌套属性集合.Add(对象属性.Name, 属性值);
                                }
                            }
                    }
                }
                var 对象字段集合 = 对象类型.GetFields(BindingFlags.Instance | BindingFlags.Public);//获取所有字段，但不包括静态字段
                foreach (var 对象字段 in 对象字段集合)
                {
                    if (序列化指定属性 == null || 序列化指定属性.Contains(对象字段.Name))
                        if (!Attribute.IsDefined(对象字段, typeof(Xaml序列化忽略)))
                        {
                            var 字段值 = 对象字段.GetValue(对象);
                            if (字段值 == null) //检查值是否为空
                            {
                                新节点.属性集合.Add(对象字段.Name, "null");
                            }
                            else if (对象字段.FieldType.IsValueType || 对象字段.FieldType == typeof(string) || 对象字段.FieldType.IsEnum) //属性是值类型，包括结构体，要保证对象正确的实现了tostring的方法
                            {
                                新节点.属性集合.Add(对象字段.Name, 字段值.ToString()!);
                            }
                            else
                            {
                                嵌套属性集合.Add(对象字段.Name, 字段值);
                            }
                        }
                }
                foreach (var 嵌套属性 in 嵌套属性集合)
                {
                    Xaml节点 属性节点 = new Xaml节点(嵌套属性.Key);
                    新节点.子级集合.Add(属性节点);
                    if (对象递归序列化Xaml(嵌套属性.Value) is Xaml节点 属性Xaml对象)
                        属性节点.子级集合.Add(属性Xaml对象);
                }
                return 新节点;
            }
        }
    }

    /// <summary>
    /// 实例化的内容放这里
    /// </summary>
    public static partial class Xaml序列化
    {
        public static T? 实例化<T>(string 原始字符串) => 实例化<T>(原始字符串, null, null);
        public static T? 实例化<T>(string 原始字符串, Func<Type, object?>? 指定类型实例化, 属性实例化委托? 指定属性实例化)
        {
            var 类型 = typeof(T);
            Xaml解析器 xaml = Xaml解析器.解析字符串(原始字符串)!;
            return (T?)自定义类型实例化(xaml.根节点.子级集合[0], 指定类型实例化, 指定属性实例化);
        }
        public static T? 实例化<T>(Xaml节点 节点) => 实例化<T>(节点, null, null);
        public static T? 实例化<T>(Xaml节点 节点, Func<Type, object?>? 指定类型实例化, 属性实例化委托? 指定属性实例化)
        {
            return (T?)自定义类型实例化(节点, 指定类型实例化, 指定属性实例化);
        }

        public static T 创建副本<T>(object 对象) => 创建副本<T>(对象, null, null);
        public static T 创建副本<T>(object 对象, Func<Type, object?>? 指定类型实例化, 属性实例化委托? 指定属性实例化)
        {
            if (对象 == null)
            {
                throw new Exception("对象不能为空");
            }
            if (创建副本(对象, 指定类型实例化, 指定属性实例化) is T 物体)
            {
                return 物体;
            }
            else
            {
                throw new Exception("类型不匹配");
            }
        }

        public static object? 创建副本(object 对象) => 创建副本(对象, null, null);
        public static object? 创建副本(object 对象, Func<Type, object?>? 指定类型实例化, 属性实例化委托? 指定属性实例化)
        {
            var 序列化字符串 = Xaml序列化.序列化(对象);
            Xaml解析器 xaml = Xaml解析器.解析字符串(序列化字符串)!;
            return 自定义类型实例化(xaml.根节点.子级集合[0], 指定类型实例化, 指定属性实例化);
        }
        /// <summary>
        /// 指定属性实例化函数
        /// </summary>
        /// <param name="对象类型"></param>
        /// <param name="对象属性"></param>
        /// <param name="属性值"></param>
        /// <returns>返回True则使用当前返回的属性值，返回False则继续执行系统默认的序列化属性值</returns>
        public delegate bool 属性实例化委托(Type 对象类型, PropertyInfo 对象属性, out object? 属性值);

        static Dictionary<Guid, object?> 循环引用字典 = [];

        static List<Action> 循环引用赋值 = [];

        public static object? 自定义类型实例化(Xaml节点 节点, Func<Type, object?>? 指定类型实例化, 属性实例化委托? 指定属性实例化)
        {
            循环引用赋值 = [];
            循环引用字典 = [];
            var 值 = 自定义类型实例化过程(节点, 指定类型实例化, 指定属性实例化);
            foreach (var 循环引用 in 循环引用赋值)
                循环引用.Invoke();
            循环引用赋值 = [];
            循环引用字典 = [];
            return 值;
        }
        /// <summary>
        /// 自定义实例化的过程
        /// </summary>
        /// <param name="节点">Xaml节点</param>
        /// <param name="对象类型">实例化对象的类型</param>
        /// <param name="指定类型实例化">回调函数,由调用者指定类型的实例化，并反回调返回实例化后的值</param>
        /// <param name="指定属性实例化">回调函数,由调用者判断属性是否应该实例化，返回True则需要，返回False则不实例化</param>
        /// <returns>返回实例化后的对象</returns>
        /// <exception cref="Exception">所有实例化的类必须拥有无参构造函数！</exception>
        private static object? 自定义类型实例化过程(Xaml节点 节点, Func<Type, object?>? 指定类型实例化, 属性实例化委托? 指定属性实例化)
        {
            if (节点.名称 == "GUID")
            {
                var GUID = Guid.Parse(节点.内容);
                if (循环引用字典.ContainsKey(GUID))
                    return 循环引用字典[GUID];
                else
                    return null;
            }
            Type 对象类型 = 实例化类型(节点.名称);
            if (指定类型实例化 != null)
            {
                var 实例化值 = 指定类型实例化.Invoke(对象类型);
                if (节点.属性集合.ContainsKey("GUID"))
                    循环引用字典.Add(Guid.Parse(节点.属性集合["GUID"]), 实例化值);
                return 实例化值;
            }
            else if (Xaml序列化.是否包含特性方法(对象类型, typeof(Xaml实例化方法), out var 实例化方法, BindingFlags.Static | BindingFlags.Public))//判断该类是否有静态的实例化方法
            {
                var 实例化值 = 实例化方法!.Invoke(null, [节点]);
                if (节点.属性集合.ContainsKey("GUID")) 循环引用字典.Add(Guid.Parse(节点.属性集合["GUID"]), 实例化值);
                return 实例化值;
            }
            else
            {
                if (检查是基础类型(对象类型))
                {
                    return 基础实例化(对象类型, 节点.内容);
                }
                if (对象类型.IsGenericType)//检查对象是否是泛型
                {
                    var 泛型类型 = 获取泛型类型(对象类型);
                    if (泛型类型 == typeof(Dictionary<,>))
                    {
                        var 字典实例 = 创建类型对象(对象类型);
                        if (节点.属性集合.ContainsKey("GUID")) 循环引用字典.Add(Guid.Parse(节点.属性集合["GUID"]), 字典实例);

                        var Add方法 = 对象类型.GetMethod("Add")!;
                        for (int i = 0; i < 节点.子级集合[0].子级集合.Count; i++)
                        {
                            var 键 = 自定义类型实例化过程(节点.子级集合[0].子级集合[i], 指定类型实例化, 指定属性实例化);
                            var 值 = 自定义类型实例化过程(节点.子级集合[1].子级集合[i], 指定类型实例化, 指定属性实例化);
                            Add方法.Invoke(字典实例, [键, 值]);
                        }
                        return 字典实例;
                    }
                    else if (泛型类型 == typeof(HashSet<>) || 泛型类型 == typeof(List<>))//例如哈希表
                    {
                        object 对象实例 = 创建类型对象(对象类型);
                        if (节点.属性集合.ContainsKey("GUID")) 循环引用字典.Add(Guid.Parse(节点.属性集合["GUID"]), 对象实例);

                        if (对象实例.GetType().GetMethod("Add") is MethodInfo Add方法)
                        {
                            for (int i = 0; i < 节点.子级集合.Count; i++)
                            {
                                var 元素 = 自定义类型实例化过程(节点.子级集合[i], 指定类型实例化, 指定属性实例化);
                                Add方法.Invoke(对象实例, [元素]);
                            }
                            return 对象实例;
                        }
                        else
                        {
                            throw new Exception($"错误，该类型:{对象类型},没有Add方法");
                        }
                    }
                }
                else if (对象类型.IsArray) // 对象是数组
                {
                    var 数组元素类型 = 对象类型.GetElementType()!;
                    Array 数组 = Array.CreateInstance(数组元素类型, 节点.子级集合.Count);
                    if (节点.属性集合.ContainsKey("GUID")) 循环引用字典.Add(Guid.Parse(节点.属性集合["GUID"]), 数组);
                    for (int i = 0; i < 节点.子级集合.Count; i++)
                    {
                        var 数组元素值 = 自定义类型实例化过程(节点.子级集合[i], 指定类型实例化, 指定属性实例化);
                        数组.SetValue(数组元素值, i);
                    }
                    return 数组;
                }
                if (对象类型.GetConstructor(Type.EmptyTypes) == null) //该类型没有无参构造函数，返回null
                    throw new Exception($"该类型[{对象类型.Name}]没有无参构造函数！");
                var 新对象 = Activator.CreateInstance(对象类型);//使用反射实例化类
                if (节点.属性集合.ContainsKey("GUID"))
                    循环引用字典.Add(Guid.Parse(节点.属性集合["GUID"]), 新对象);

                Dictionary<string, (PropertyInfo 属性, int 顺序)> 属性关联集合 = [];
                foreach (PropertyInfo 属性 in 对象类型.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    int 顺序 = 0;
                    Xaml顺序? 属性顺序特性 = (Xaml顺序?)Attribute.GetCustomAttribute(属性, typeof(Xaml顺序));
                    if (属性顺序特性 != null)
                    {
                        顺序 = 属性顺序特性.顺序;
                    }
                    属性关联集合.Add(属性.Name, (属性, 顺序));
                }
                List<(Action 实例化委托, string 属性名称, int 顺序)> 实例化委托集合 = [];
                foreach (var 嵌套属性 in 节点.子级集合)
                {
                    if (属性关联集合.ContainsKey(嵌套属性.名称))
                    {
                        var 属性 = 属性关联集合[嵌套属性.名称].属性;
                        var 顺序 = 属性关联集合[嵌套属性.名称].顺序;
                        Action 实例化委托 = () =>
                        {
                            var 值 = 自定义类型实例化过程(嵌套属性.子级集合[0], 指定类型实例化, 指定属性实例化);
                            属性.SetValue(新对象, 值);
                        };
                        实例化委托集合.Add((实例化委托, 嵌套属性.名称, 顺序));
                    }
                }
                foreach (var _属性 in 属性关联集合)
                {
                    var 属性 = _属性.Value.属性;
                    var 顺序 = _属性.Value.顺序;
                    if (节点.属性集合.ContainsKey(属性.Name))
                    {
                        var 属性字符串 = 节点.属性集合[属性.Name];
                        if (指定属性实例化 == null)
                        {
                            Action 实例化委托 = () =>
                            {
                                //var 值 = 自定义类型实例化(节点.属性集合[属性.Name], 指定类型实例化, 指定属性实例化);
                                //属性.SetValue(新对象, 值);
                                属性.SetValue(新对象, 基础实例化(属性.PropertyType, 属性字符串));
                            };
                            实例化委托集合.Add((实例化委托, _属性.Key, 顺序));
                        }
                        else
                        {
                            Action 实例化委托 = () =>
                            {
                                if (指定属性实例化(对象类型, 属性, out var 属性值))
                                {
                                    if (属性值 == null || 属性值.GetType() == 属性.PropertyType)
                                    {
                                        属性.SetValue(新对象, 属性值);
                                    }
                                    else
                                    {
                                        throw new Exception("自定义属性返回值类型与目标属性类型不匹配！");
                                    }
                                }
                                else
                                {
                                    属性.SetValue(新对象, 基础实例化(属性.PropertyType, 节点.属性集合[属性.Name]));
                                }

                            };
                            实例化委托集合.Add((实例化委托, _属性.Key, 顺序));
                        }

                    }
                }
                实例化委托集合.Sort((x, y) => x.顺序.CompareTo(y.顺序)); //序列化排序
                foreach (var 实例化委托 in 实例化委托集合)
                    实例化委托.实例化委托.Invoke();


                return 新对象;
            }
        }


    }

    /// <summary>
    /// 定义内部常用方法
    /// </summary>
    public static partial class Xaml序列化
    {
        private static MethodInfo 基础类型泛型实例化方法;
        private static Dictionary<Type, MethodInfo> 基础类型实例化方法集合 = [];
        static Xaml序列化()
        {
            基础类型泛型实例化方法 = typeof(Xaml序列化).GetMethod(nameof(基础类型实例化))!;//public公开方法
        }

        /// <summary>
        /// <类型，<类型的特性,该特性关联的方法>
        /// </summary>
        private static Dictionary<Type, Dictionary<Type, MethodInfo>> 类包含特性方法字典 = [];
        private static bool 是否包含特性方法(Type 对象类型, Type 特性类型, out MethodInfo? 反射对象, BindingFlags 方法作用域 = BindingFlags.Default)
        {
            if (!类包含特性方法字典.ContainsKey(对象类型))
            {
                var 对象方法集合 = 对象类型.GetMethods(方法作用域);
                Dictionary<Type, MethodInfo> _特性方法字典 = [];
                foreach (var 对象方法 in 对象方法集合)
                {
                    foreach (Attribute 特性 in 对象方法.GetCustomAttributes())
                    {
                        var _特性类型 = 特性.GetType();
                        if (_特性方法字典.ContainsKey(_特性类型))
                            _特性方法字典.Add(_特性类型, 对象方法);
                    }

                }
                类包含特性方法字典.Add(对象类型, _特性方法字典);
            }

            var 特性方法字典 = 类包含特性方法字典[对象类型];
            if (特性方法字典.ContainsKey(特性类型))
            {
                反射对象 = 特性方法字典[特性类型];
                return true;
            }
            else
            {
                反射对象 = null;
                return false;
            }

        }
        private static bool 检查类型是否继承接口(Type 类型, Type 接口)
        {
            foreach (Type interfaceType in 类型.GetInterfaces())
            {
                if (interfaceType == 接口)
                {
                    return true;
                }
            }
            return false;
        }
        private static bool 检查是基础类型(Type 类型)
        {
            return 类型.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISpanParsable<>));
        }
        /// <summary>
        /// 实例化基本类型，所有带TryParse的类型都可以，如果不是该类型侧判断是否有实例化的特性
        /// </summary>
        /// <param name="类型"></param>
        /// <param name="字符串"></param>
        /// <returns></returns>
        private static object? 基础实例化(Type 类型, string 字符串)
        {
            if (字符串 == "null")
            {
                return null;
            }
            if (类型 == typeof(string))
            {
                return 字符串;
            }
            if (是否包含特性方法(类型, typeof(Xaml实例化方法), out var 反射方法))
            {
                return 反射方法!.Invoke(null, [字符串])!;
            }
            if (!基础类型实例化方法集合.ContainsKey(类型))
            {
                MethodInfo 实例化方法 = 基础类型泛型实例化方法.MakeGenericMethod([类型]);
                基础类型实例化方法集合.Add(类型, 实例化方法);
            }
            return 基础类型实例化方法集合[类型].Invoke(null, [字符串])!;
        }
        public static T? 基础类型实例化<T>(string 字符串) where T : ISpanParsable<T>?
        {
            if (T.TryParse(字符串, null, out var 值))
            {
                return 值;
            }
            else
                return default;
        }
        public static object 创建类型对象(Type 类型)
        {
            // 获取构造函数
            ConstructorInfo 构造函数 = 类型.GetConstructor(Type.EmptyTypes)!;
            // 创建实例
            return 构造函数.Invoke(null);
        }
        public static string 序列化类型(this Type 类型)
        {
            var 序列化字符串 = 类型.Namespace + "." + 类型.Name;
            if (类型.IsGenericType)//判断是否是泛型
            {
                Type[] 泛型元素类型 = 类型.GetGenericArguments();
                序列化字符串 += "[";
                for (int i = 0; i < 泛型元素类型.Length; i++)
                {
                    序列化字符串 += 序列化类型(泛型元素类型[i]);
                    if (i < 泛型元素类型.Length - 1)
                    {
                        序列化字符串 += ",";
                    }
                }
                序列化字符串 += "]";
                return 序列化字符串;
            }
            else
            {
                return 序列化字符串;
            }

        }
        public static Type 实例化类型(string 字符串)
        {
            if (Type.GetType(字符串) is Type 类型)
            {
                return 类型;
            }
            else
            {
                int 开始索引 = 0;
                var 类型树 = 泛型类型解析(ref 字符串, ref 开始索引)[0];
                return 实例化类型(类型树);
            }
        }
        private static Type 实例化类型(Xaml节点 类型树)
        {
            if (类型树.子级集合.Count == 0)
            {
                if (Type.GetType(类型树.名称) is Type 类型)
                    return 类型;
                throw new Exception($"获取类型出错，不存在该类型:{类型树.名称}");
            }
            else
            {
                if (Type.GetType(类型树.名称) is Type 泛型基类)
                {
                    List<Type> 泛型集合 = [];
                    foreach (var 子级 in 类型树.子级集合)
                        泛型集合.Add(实例化类型(子级));
                    Type 泛型类型 = 泛型基类.MakeGenericType(泛型集合.ToArray());
                    return 泛型类型;
                }
                throw new Exception($"获取类型出错，不存在该类型:{类型树.名称}");
            }
        }
        private static Xaml节点[] 泛型类型解析(ref string 字符串, ref int 当前索引)
        {
            List<Xaml节点> 节点集合 = [];
            while (当前索引 < 字符串.Length)
            {
                if (查找类型名(ref 字符串, ref 当前索引, out var 类型名))
                {
                    var 新节点 = new Xaml节点(类型名);
                    节点集合.Add(新节点);

                    if (当前索引 >= 字符串.Length) return 节点集合.ToArray();

                    if (字符串[当前索引] == '[')
                    {
                        当前索引++;
                        新节点.子级集合.AddRange(泛型类型解析(ref 字符串, ref 当前索引));
                        if (当前索引 >= 字符串.Length) return 节点集合.ToArray();
                    }
                    if (字符串[当前索引] == ']')
                    {
                        当前索引++;
                        return 节点集合.ToArray();
                    }
                    if (字符串[当前索引] == ',')
                        当前索引++;
                }
                else
                {
                    当前索引++;
                }
            }
            return 节点集合.ToArray();
        }
        private static bool 查找类型名(ref string 字符串, ref int 当前索引, out string 类型名)
        {
            类型名 = "";
            int 开始索引 = 当前索引;
            while (当前索引 < 字符串.Length)
            {
                var 当前字符 = 字符串[当前索引];
                if (当前字符 == '[' || 当前字符 == ']' || 当前字符 == ',')
                {
                    if (当前索引 - 开始索引 == 0)
                    {
                        return false;
                    }
                    else
                    {
                        类型名 = 字符串.Substring(开始索引, 当前索引 - 开始索引);
                        return true;
                    }

                }
                当前索引++;
            }
            if (当前索引 - 开始索引 == 0)
            {
                return false;
            }
            else
            {
                类型名 = 字符串.Substring(开始索引, 当前索引 - 开始索引);
                return true;
            }
        }

        /// <summary>
        /// 获取一个泛型元素类型的，泛型类型
        /// 例如：List<int> ，返回List<T>
        /// </summary>
        /// <param name="泛型元素类型">例如：List<int></param>
        /// <returns>返回List<T></returns>
        public static Type 获取泛型类型(Type 泛型元素类型)
        {
            if (泛型元素类型.IsGenericType)//检查对象是否是泛型
            {
                return 泛型元素类型.GetGenericTypeDefinition();
            }
            throw new Exception("该类型不是泛型元素类型");
        }
    }

    /// <summary>
    /// 将字符串解析成XAML节点
    /// </summary>
    public class Xaml解析器
    {
        #region 解析方法
        public static Xaml解析器? 解析字符串(string 原始字符串)
        {
            Xaml解析器 xaml = new Xaml解析器(原始字符串);
            xaml.根节点?.向下递归(x =>
            {
                foreach (var 子级 in x.子级集合)
                    子级.父级节点 = x;
            });
            return xaml;
        }

        #endregion
        #region 解析过程
        static HashSet<char> 所有特殊字符 = ['!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '=', '+', '[', ']', '{', '}', '\\', '|', ';', ':', '\'', '"', ',', '<', '.', '>', '/', '?'];
        static HashSet<char> 可以带点 = ['!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '=', '+', '[', ']', '{', '}', '\\', '|', ';', ':', '\'', '"', ',', '<', '>', '/', '?'];
        static HashSet<char> 可以带点号冒号减号 = ['!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '=', '{', '}', '\\', '|', ';', '\'', '"', '<', '>', '/', '?'];
        static HashSet<char> 空字符 = [' ', '\r', '\n', '\t',];

        public Xaml节点 根节点 { get; private set; }
        private string 原始字符串;
        private int 当前索引;
        private char 当前字符 => 原始字符串[当前索引];
        private Xaml解析器(string 原始字符串)
        {
            this.当前索引 = 0;
            this.原始字符串 = 原始字符串;
            根节点 = new Xaml节点("根节点");
            根节点.原始字符串 = 原始字符串;
            递归解析(根节点);
        }
        private string 递归解析(Xaml节点 父级节点)
        {
            while (当前索引 < 原始字符串.Length)
            {
                {
                    过滤空字符();//继续查询下一个标记
                    var 内容开始索引 = 当前索引;
                    while (当前索引 < 原始字符串.Length)
                    {
                        if (当前字符 == '<')
                        {
                            if (原始字符串[当前索引 + 1] == '/') //说明到了之前标记的末尾，返回上级递归,例如：</xaxml>
                            {
                                if (当前索引 > 内容开始索引)
                                {
                                    //这里是两个尖括号之外的内容<>...<>
                                    var 内容 = 原始字符串.Substring(内容开始索引, 当前索引 - 内容开始索引);
                                    return 内容;
                                }
                                return "";
                            }
                            else //到了新的标记开头,继续循环
                            {
                                break;
                            }
                        }
                        else
                        {
                            当前索引++;

                            //这里是两个尖括号之外的内容<>...<>
                        }
                    }
                    if (当前索引 >= 原始字符串.Length) // 完成结束 
                        break;
                }

                var 节点开始 = 当前索引;
                查找标记开始(out var 标记名称, out var 状态);//情况0:<name  情况1：:<name>  情况2：:<name/>
                过滤空字符();
                Xaml节点 新节点 = new Xaml节点(标记名称);
                父级节点.子级集合.Add(新节点);
                if (状态 == 0)
                {
                    查找属性集合(out var 属性集合, out var 标记结束);
                    新节点.属性集合 = 属性集合;
                    if (标记结束)
                    {
                        状态 = 2;
                        当前索引++;
                    }
                    else
                    {
                        状态 = 1;
                    }
                }
                if (状态 == 1) //只有状态为1时，才有子级，做递归子级操作
                {
                    当前索引++;
                    var 内容 = 递归解析(新节点);
                    if (内容.Length > 0)
                        新节点.内容 = 内容;
                    过滤空字符();//继续查询下一个标记
                    if (当前字符 == '<')
                    {
                        if (原始字符串[当前索引 + 1] == '/') //说明到了末尾
                        {
                            查找标记结束(out var 标记结束名称);
                            if (标记名称 == 标记结束名称)
                            {
                                当前索引++;
                            }
                            else
                            {
                                throw new Exception("标记结束名称与开始名称不一致！");
                            }
                        }
                        else //没到末尾，继续循环
                        {

                        }
                    }
                    else
                    {
                        throw new Exception("出错，不能在<>以外出现其他字符");
                    }
                }

                新节点.原始字符串 = 原始字符串.Substring(节点开始, 当前索引 - 节点开始);
            }
            return "";
        }
        /// <summary>
        /// 状态0：<xaml ...    
        /// 状态1：<xaml>
        /// 状态2：<xaml/>
        /// </summary>
        /// <param name="标记名称"></param>
        /// <param name="状态"></param>
        /// <exception cref="Exception"></exception>
        private void 查找标记开始(out string 标记名称, out int 状态)
        {
        重新开始:
            过滤空字符();
            if (当前字符 == '<')
            {
                if (原始字符串[当前索引 + 1] == '!' && 原始字符串[当前索引 + 2] == '-' && 原始字符串[当前索引 + 3] == '-')
                {
                    当前索引 += 4;
                    while (当前索引 < 原始字符串.Length)
                    {
                        if (当前字符 == '-' && 原始字符串[当前索引 + 1] == '-' && 原始字符串[当前索引 + 2] == '>')
                        {
                            当前索引 += 3;
                            if (当前索引 < 原始字符串.Length)
                                goto 重新开始; //标记结束,查找下一行
                            else
                            {
                                标记名称 = "";
                                状态 = 2;
                                return;
                            }

                        }
                        当前索引++;
                    }
                    if (当前索引 >= 原始字符串.Length)
                        throw new Exception("未找到结束的注释标记！");
                }
                当前索引++;
            }

            if (!查检开头是字母或者汉字(当前字符))
                throw new Exception($"<之后不能接[{当前字符}]");

            标记名称 = "";
            int 标记名开始 = 当前索引;
            bool 已检查到空格 = false;

            while (当前索引 < 原始字符串.Length)
            {
                if (当前字符 == ' ')
                {
                    标记名称 = 去掉空字符(原始字符串.Substring(标记名开始, 当前索引 - 标记名开始));
                    过滤空字符();
                    已检查到空格 = true;
                }
                else if (当前字符 == '>')
                {
                    标记名称 = 去掉空字符(原始字符串.Substring(标记名开始, 当前索引 - 标记名开始));
                    状态 = 1;
                    return;
                }
                else if (当前字符 == '/')
                {
                    当前索引++;
                    if (当前字符 == '>')
                    {
                        标记名称 = 去掉空字符(原始字符串.Substring(标记名开始, 当前索引 - 标记名开始 - 1));
                        状态 = 2;
                        return;
                    }
                    else
                        throw new Exception($"在标记名称结束的/后面出现非法字符！'{当前字符}'");
                }
                else
                {
                    if (已检查到空格)
                    {
                        状态 = 0;
                        return;
                    }
                    else
                    {
                        if (可以带点号冒号减号.Contains(当前字符))
                            throw new Exception($"在标记名称中出现非法字符！'{当前字符}'");
                        当前索引++;
                    }
                }
            }
            throw new Exception($"未找到任何单词");
        }
        private void 查找标记结束(out string 标记名称)
        {
            过滤空字符();
            if (当前字符 == '<')
            {
                当前索引++;
                if (当前字符 == '/')
                    当前索引++;
                else
                    throw new Exception($"结束标记出现错误<后面只能接/");
            }

            if (!查检开头是字母或者汉字(当前字符))
                throw new Exception($"<之后不能接[{当前字符}]");
            int 标记名开始 = 当前索引;
            while (当前索引 < 原始字符串.Length)
            {
                if (当前字符 == '>')
                {
                    标记名称 = 原始字符串.Substring(标记名开始, 当前索引 - 标记名开始);
                    return;
                }
                else if (可以带点号冒号减号.Contains(当前字符))
                    throw new Exception($"出现非法字符！'{当前字符}'");
                当前索引++;
            }
            throw new Exception($"未找到任何单词");
        }
        private void 查找注释结束()
        {
            while (当前索引 < 原始字符串.Length)
            {

            }
        }
        private void 查找属性集合(out Dictionary<string, string> 属性集合, out bool 标记结束)
        {
            属性集合 = [];
            while (当前索引 < 原始字符串.Length)
            {
                过滤空字符();
                查找属性名(out var 属性名);
                当前索引++;
                查找属性值(out var 属性值);
                属性集合.Add(属性名, 属性值);
                当前索引++;
                过滤空字符();
                if (当前字符 == '/')
                {
                    当前索引++;
                    if (当前字符 == '>')
                    {
                        标记结束 = true;
                        return;
                    }
                    else
                    {
                        throw new Exception("查找属性的时候出现了错误！/后面必须接>号");
                    }
                }
                if (当前字符 == '>')
                {
                    标记结束 = false;
                    return;
                }
            }
            throw new Exception("查找属性的时候出现了错误！");
        }
        private void 查找属性名(out string 属性名)
        {
            过滤空字符();
            if (!查检开头是字母或者汉字(当前字符))
                throw new Exception($"<之后不能接[{当前字符}]");
            int 标记名开始 = 当前索引;
            当前索引++;
            while (当前索引 < 原始字符串.Length)
            {
                if (当前字符 == '=')
                {
                    属性名 = 原始字符串.Substring(标记名开始, 当前索引 - 标记名开始);
                    return;
                }
                else if (可以带点号冒号减号.Contains(当前字符))
                    throw new Exception($"出现非法字符！'{当前字符}'");
                else
                    当前索引++;
            }
            throw new Exception($"未找到任何单词");
        }
        private void 查找属性值(out string 属性值)
        {
            过滤空字符();
            if (当前字符 == '"')
            {
                当前索引++;
                int 标记名开始 = 当前索引;
                bool 转义字符 = false;
                while (当前索引 < 原始字符串.Length)
                {
                    if (转义字符)
                    {
                        当前索引++;
                    }
                    else
                    {
                        if (当前字符 == '"')
                        {
                            属性值 = 原始字符串.Substring(标记名开始, 当前索引 - 标记名开始);
                            return;
                        }
                        else if (当前字符 == '\\')
                        {
                            转义字符 = true;
                            当前索引++;
                        }
                        else
                            当前索引++;
                    }
                }
                throw new Exception($"未找到字段结束冒号");
            }
            else
            {
                throw new Exception($"未找到任何字段");
            }
        }
        private void 过滤空字符()
        {
            while (当前索引 < 原始字符串.Length)
            {
                if (!空字符.Contains(当前字符))
                    return;
                当前索引++;
            }
        }
        private static bool 查检开头是字母或者汉字(char 字符)
        {
            return Regex.IsMatch(字符.ToString(), @"^[a-zA-Z\u4e00-\u9fa5]$");
        }
        private static string 去掉空字符(string 原字符串)
        {
            List<char> 字符集合 = new List<char>();
            foreach (var 原字符 in 原字符串)
            {
                if (!空字符.Contains(原字符))
                    字符集合.Add(原字符);
            }
            return new string(字符集合.ToArray());
        }
        #endregion
        #region XAML序列化
        public string 序列化()
        {
            StringBuilder 序列化字符串 = new StringBuilder();
            foreach (var 子级 in 根节点.子级集合)
                子级.格式化输出(序列化字符串);
            return 序列化字符串.ToString();
        }
        #endregion
    }

    /// <summary>
    /// 具有树形结构的XAML节点
    /// </summary>
    public class Xaml节点
    {
        public string 原始字符串 { get; set; } = string.Empty;
        public string 名称 { get; set; } = string.Empty;
        public string 内容 { get; set; } = string.Empty;
        public Dictionary<string, string> 属性集合 { get; set; } = [];
        public Xaml节点? 父级节点 { get; set; }

        public List<Xaml节点> 子级集合 { get; set; } = [];
        public Xaml节点(string 名称)
        {
            this.名称 = 名称;
        }

        public void 向上递归(Action<Xaml节点> 递归委托)
        {
            if (this.父级节点 != null)
            {
                递归委托.Invoke(父级节点);
                this.父级节点.向上递归(递归委托);
            }
        }
        public void 向下递归(Action<Xaml节点> 递归委托)
        {
            递归委托.Invoke(this);
            foreach (var 子级 in 子级集合)
                子级.向下递归(递归委托);
        }
        public void 向上递归(Func<Xaml节点, bool> 递归委托)
        {
            if (this.父级节点 != null && 递归委托.Invoke(父级节点))
            {
                this.父级节点.向上递归(递归委托);
            }
        }
        public void 向下递归(Func<Xaml节点, bool> 递归委托)
        {
            if (递归委托.Invoke(this))
                foreach (var 子级 in 子级集合)
                    子级.向下递归(递归委托);
        }
        public T? 获取属性值<T>(string 属性名称)
        {
            if (属性集合.ContainsKey(属性名称))
            {
                var 属性类型 = typeof(T);
                if (属性类型 == typeof(int))
                {
                    return (dynamic)int.Parse(属性集合[属性名称]);
                }
                else if (属性类型 == typeof(float))
                {
                    return (dynamic)float.Parse(属性集合[属性名称]);
                }
                else if (属性类型 == typeof(double))
                {
                    return (dynamic)double.Parse(属性集合[属性名称]);
                }
                else if (属性类型 == typeof(string))
                {
                    return (dynamic)属性集合[属性名称];
                }
                else
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }

        public virtual string 格式化输出()
        {
            StringBuilder 序列化字符串 = new StringBuilder();
            格式化输出(序列化字符串);
            return 序列化字符串.ToString();
        }
        public virtual void 格式化输出(StringBuilder 序列化字符串, string 缩进 = "")
        {
            序列化字符串.Append("<");
            序列化字符串.Append(名称);
            foreach (var item in 属性集合)
            {
                序列化字符串.Append(" ");
                序列化字符串.Append(item.Key);
                序列化字符串.Append("=\"");
                序列化字符串.Append(item.Value);
                序列化字符串.Append("\"");
            }
            if (子级集合.Count == 0)
            {
                if (内容.Length == 0)
                {
                    序列化字符串.Append("/>");
                }
                else
                {
                    序列化字符串.Append(">");
                    序列化字符串.Append(内容);
                    序列化字符串.Append($"</{名称}>");
                }

            }
            else
            {
                序列化字符串.Append(">");
                foreach (var 子级 in 子级集合)
                {
                    序列化字符串.Append("\r\n");
                    序列化字符串.Append(缩进 + "     ");
                    子级.格式化输出(序列化字符串, 缩进 + "     ");
                }
                序列化字符串.Append("\r\n");
                序列化字符串.Append(缩进);
                序列化字符串.Append($"</{名称}>");
            }
        }
        public override string ToString()
        {
            StringBuilder 序列化字符串 = new StringBuilder();
            格式化输出(序列化字符串);
            return 序列化字符串.ToString();
        }
    }

    #region  特性标记
    /// <summary>
    /// 必须为静态方法,参数为 Xaml节点,返回所属类形的实例
    /// </summary>
    public class Xaml实例化方法 : Attribute
    {
    }
    /// <summary>
    /// 静态方法,无参数，返回序列化后的string字符串
    /// </summary>
    public class Xaml序列化方法 : Attribute
    {
    }
    /// <summary>
    /// 静态方法,无参数，返回 HashSet<string> 属性哈希集合
    /// </summary>
    public class Xaml序列化指定属性 : Attribute
    {
    }
    /// <summary>
    /// 无参函数 ，返回真则序列化，假不序列化
    /// </summary>
    public class Xaml序列化类判断 : Attribute
    {
    }
    /// <summary>
    /// 序列化的顺序
    /// </summary>
    public class Xaml顺序 : Attribute
    {
        public int 顺序 { get; set; } = 0;
        public Xaml顺序(int 顺序)
        {
            this.顺序 = 顺序;
        }
    }
    /// <summary>
    /// 忽略该属性的序列化
    /// </summary>
    public class Xaml序列化忽略 : Attribute
    {

    }
    #endregion
}
