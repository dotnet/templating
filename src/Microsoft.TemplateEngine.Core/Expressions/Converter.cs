using System;
using System.Reflection;

namespace Microsoft.TemplateEngine.Core.Expressions
{
    public static class Converter
    {
        static Converter()
        {
            Register(new BoolConverter());
            Register(new IntConverter());
            Register(new LongConverter());
            Register(new FloatConverter());
            Register(new DoubleConverter());
            Register(new StringConverter());
        }

        private static void Register<T>(ConverterItem<T> item)
        {
            ConverterItem<T>.IsHandledBy(item);
        }

        public static bool TryConvert<T>(object source, out T result)
        {
            return ConverterItem<T>.TryExecute(source, out result);
        }

        private abstract class ConverterItem<T>
        {
            public static void IsHandledBy<TInstance>(TInstance instance)
                where TInstance : ConverterItem<T>
            {
                Instance = instance;
            }

            public static ConverterItem<T> Instance { get; private set; }

            public static bool TryExecute(object source, out T result)
            {
                if (source is T x)
                {
                    result = x;
                    return true;
                }

                T handlerValue = default(T);
                bool? handlerResult = Instance?.TryExecuteInternal(source, out handlerValue);

                if (handlerResult.HasValue)
                {
                    result = handlerValue;
                    return handlerResult.Value;
                }

                if (typeof(T).GetTypeInfo().IsEnum && source is string s)
                {
                    try
                    {
                        result = (T)Enum.Parse(typeof(T), s, true);
                        return true;
                    }
                    catch
                    {
                    }
                }

                try
                {
                    result = (T)Convert.ChangeType(source, typeof(T));
                    return true;
                }
                catch
                {
                    result = default(T);
                    return false;
                }
            }

            protected abstract bool? TryExecuteInternal(object source, out T result);
        }

        private class BoolConverter : ConverterItem<bool>
        {
            protected override bool? TryExecuteInternal(object source, out bool result)
            {
                if (source is string s)
                {
                    return bool.TryParse(s, out result);
                }

                result = false;
                return null;
            }
        }

        private class IntConverter : ConverterItem<int>
        {
            protected override bool? TryExecuteInternal(object source, out int result)
            {
                if (source is string s)
                {
                    return int.TryParse(s, out result);
                }

                result = 0;
                return null;
            }
        }

        private class LongConverter : ConverterItem<long>
        {
            protected override bool? TryExecuteInternal(object source, out long result)
            {
                if (source is string s)
                {
                    return long.TryParse(s, out result);
                }

                result = 0;
                return null;
            }
        }

        private class FloatConverter : ConverterItem<float>
        {
            protected override bool? TryExecuteInternal(object source, out float result)
            {
                if (source is string s)
                {
                    return float.TryParse(s, out result);
                }

                result = 0;
                return null;
            }
        }

        private class DoubleConverter : ConverterItem<double>
        {
            protected override bool? TryExecuteInternal(object source, out double result)
            {
                if (source is string s)
                {
                    return double.TryParse(s, out result);
                }

                result = 0;
                return null;
            }
        }

        private class StringConverter : ConverterItem<string>
        {
            protected override bool? TryExecuteInternal(object source, out string result)
            {
                result = source?.ToString();
                return true;
            }
        }
    }
}
