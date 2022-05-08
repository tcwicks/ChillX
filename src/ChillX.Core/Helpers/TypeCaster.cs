using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ChillX.Core.Helpers
{
    /// <summary>
    /// Non boxing type caster using expression trees
    /// Converts from <see cref="TSrc"/> to <see cref="TDest"/>
    /// </summary>
    /// <typeparam name="TSrc">Source type to convert from</typeparam>
    /// <typeparam name="TDest">Destination type to convert to</typeparam>
    public static class TypeCaster<TSrc, TDest>
    {
        /// <summary>
        /// Performs a non boxing cast from type <see cref="TSrc"/> to <see cref="TDest"/>
        /// </summary>
        /// <param name="from">Source value of type <see cref="TSrc"/> you want to convert from</param>
        /// <returns>Converted value cast to type <see cref="TDest"/></returns>
        public static TDest Convert(TSrc from)
        {
            return caster(from);
        }

        private static readonly Func<TSrc, TDest> caster = Get();

        private static Func<TSrc, TDest> Get()
        {
            ParameterExpression sourceParameter = Expression.Parameter(typeof(TSrc));
            UnaryExpression converter = Expression.ConvertChecked(sourceParameter, typeof(TDest));
            return Expression.Lambda<Func<TSrc, TDest>>(converter, sourceParameter).Compile();
        }
    }
}
