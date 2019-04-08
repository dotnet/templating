using System.Linq;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal class Wrapper
    {
        public static IWrapper<T, T> For<T>(Chain<JsonBuilder<IWrapper<T, T>, Wrapper<T, T>>> builderConfigurer)
            where T : new()
            => new Wrapper<T, T>(builderConfigurer);

        public static IWrapper<T, TConcrete> For<T, TConcrete>(Chain<JsonBuilder<IWrapper<T, TConcrete>, Wrapper<T, TConcrete>>> builderConfigurer)
            where TConcrete : T
            => new Wrapper<T, TConcrete>(builderConfigurer);
    }

    internal class Wrapper<T, TConcrete> : IWrapper<T, TConcrete>
        where TConcrete : T
    {
        private readonly IJsonBuilder<IWrapper<T, TConcrete>> _builder;

        public Wrapper(Chain<JsonBuilder<IWrapper<T, TConcrete>, Wrapper<T, TConcrete>>> builderConfigurer)
        {
            _builder = builderConfigurer(new JsonBuilder<IWrapper<T, TConcrete>, Wrapper<T, TConcrete>>(() => new Wrapper<T, TConcrete>(builderConfigurer)));
        }

        private Wrapper()
        {
        }

        public T Value { get; set; }

        public TConcrete Deserialize(IJsonToken o)
        {
            IJsonObject temp = o.Factory.CreateObject();
            temp.SetValue("Value", o);
            IWrapper<T, TConcrete> wrapper = _builder.Deserialize(temp);
            return (TConcrete)wrapper.Value;
        }

        public IJsonToken Serialize(IJsonDocumentObjectModelFactory f, T o)
        {
            Wrapper<T, TConcrete> wrapped = new Wrapper<T, TConcrete>() { Value = o };
            IJsonObject result = _builder.Serialize(f, wrapped);
            return result.Properties().FirstOrDefault().Value;
        }
    }
}
