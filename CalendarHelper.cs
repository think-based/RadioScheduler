//Be Naame Khoda
//FileName: CalendarHelper.cs

using System;
using System.Globalization;

public static class CalendarHelper
{
    public static DateTime ConvertDate(DateTime date, string calendarType)
    {
        switch (calendarType)