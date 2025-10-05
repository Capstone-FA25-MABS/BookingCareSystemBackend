using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Shared.Common.Enums;
using System;
using System.Collections.Generic;

namespace BookingCare.Services.Schedule.Utilities;

internal static class AppointmentTimeHelper
{
    public static AppointmentTimeDto ConvertEnumToDto(AppointmentTime appointmentTime)
    {
        var (startTime, endTime) = GetTimeStringsFromEnum(appointmentTime);
        return new AppointmentTimeDto
        {
            Id = GenerateDeterministicGuid((int)appointmentTime),
            StartTime = startTime,
            EndTime = endTime
        };
    }

    internal static Guid GenerateDeterministicGuid(int value)
    {
        byte[] guidBytes = new byte[16];
        byte[] valueBytes = BitConverter.GetBytes(value);

        Array.Copy(valueBytes, 0, guidBytes, 0, 4);
        for (int i = 4; i < 16; i++)
        {
            guidBytes[i] = (byte)(0xA0 + (i % 16));
        }

        return new Guid(guidBytes);
    }

    internal static (string startTime, string endTime) GetTimeStringsFromEnum(AppointmentTime appointmentTime)
    {
        // Parse enum name to derive start/end time to avoid repeating literal strings
        var name = appointmentTime.ToString();
        // Expected format: AT_HH_MM_HH_MM or AT_HH_MM_HH_MM
        var parts = name.Split('_');
        try
        {
            if (parts.Length >= 5)
            {
                // parts: ["AT", "HH", "MM", "HH", "MM"]
                var start = $"{parts[1]}:{parts[2]}";
                var end = $"{parts[3]}:{parts[4]}";
                return (start, end);
            }
        }
        catch
        {
            // fall through to fallback
        }

        // Fallback to default unknown
        return ("Unknown", "Unknown");
    }
}
