﻿using Jvedio.Mapper.BaseMapper;
using System;
using System.Linq;

namespace Jvedio.Test
{
    public static class TestMapper
    {
        public static void insertAndSelect<T>(BaseMapper<T> mapper)
        {
            System.Reflection.PropertyInfo[] propertyInfos = typeof(T).GetType().GetProperties();
            for (int i = 0; i < 10; i++)
            {
                T item = System.Activator.CreateInstance<T>();
                item.GetType().GetProperties().ToList().ForEach(prop =>
                {
                    if (prop.PropertyType.IsEnum)
                    {
                        prop.SetValue(item, i);
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(item, $"{prop.Name } {i}");
                    }
                });
                try
                {
                    mapper.Insert(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
            //mapper.selectAll().ForEach(item => Console.WriteLine(item));
        }
    }
}
