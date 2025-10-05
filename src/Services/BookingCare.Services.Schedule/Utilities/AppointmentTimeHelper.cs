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
        return appointmentTime switch
        {
            // Range time 30 minutes
            AppointmentTime.AT_08_00_08_30 => ("08:00", "08:30"),
            AppointmentTime.AT_08_30_09_00 => ("08:30", "09:00"),
            AppointmentTime.AT_09_00_09_30 => ("09:00", "09:30"),
            AppointmentTime.AT_09_30_10_00 => ("09:30", "10:00"),
            AppointmentTime.AT_10_00_10_30 => ("10:00", "10:30"),
            AppointmentTime.AT_10_30_11_00 => ("10:30", "11:00"),
            AppointmentTime.AT_11_00_11_30 => ("11:00", "11:30"),
            AppointmentTime.AT_11_30_12_00 => ("11:30", "12:00"),
            AppointmentTime.AT_13_00_13_30 => ("13:00", "13:30"),
            AppointmentTime.AT_13_30_14_00 => ("13:30", "14:00"),
            AppointmentTime.AT_14_00_14_30 => ("14:00", "14:30"),
            AppointmentTime.AT_14_30_15_00 => ("14:30", "15:00"),
            AppointmentTime.AT_15_00_15_30 => ("15:00", "15:30"),
            AppointmentTime.AT_15_30_16_00 => ("15:30", "16:00"),
            AppointmentTime.AT_16_00_16_30 => ("16:00", "16:30"),
            AppointmentTime.AT_16_30_17_00 => ("16:30", "17:00"),
            AppointmentTime.AT_17_00_17_30 => ("17:00", "17:30"),
            AppointmentTime.AT_17_30_18_00 => ("17:30", "18:00"),
            AppointmentTime.AT_18_00_18_30 => ("18:00", "18:30"),
            AppointmentTime.AT_18_30_19_00 => ("18:30", "19:00"),
            AppointmentTime.AT_19_00_19_30 => ("19:00", "19:30"),
            AppointmentTime.AT_19_30_20_00 => ("19:30", "20:00"),
            AppointmentTime.AT_20_00_20_30 => ("20:00", "20:30"),
            AppointmentTime.AT_20_30_21_00 => ("20:30", "21:00"),
            AppointmentTime.AT_21_00_21_30 => ("21:00", "21:30"),
            AppointmentTime.AT_21_30_22_00 => ("21:30", "22:00"),
            AppointmentTime.AT_22_00_22_30 => ("22:00", "22:30"),
            AppointmentTime.AT_22_30_23_00 => ("22:30", "23:00"),

            // Range time one hour
            AppointmentTime.AT_08_00_09_00 => ("08:00", "09:00"),
            AppointmentTime.AT_09_00_10_00 => ("09:00", "10:00"),
            AppointmentTime.AT_10_00_11_00 => ("10:00", "11:00"),
            AppointmentTime.AT_11_00_12_00 => ("11:00", "12:00"),
            AppointmentTime.AT_13_00_14_00 => ("13:00", "14:00"),
            AppointmentTime.AT_14_00_15_00 => ("14:00", "15:00"),
            AppointmentTime.AT_15_00_16_00 => ("15:00", "16:00"),
            AppointmentTime.AT_16_00_17_00 => ("16:00", "17:00"),
            AppointmentTime.AT_17_00_18_00 => ("17:00", "18:00"),
            AppointmentTime.AT_18_00_19_00 => ("18:00", "19:00"),
            AppointmentTime.AT_19_00_20_00 => ("19:00", "20:00"),
            AppointmentTime.AT_20_00_21_00 => ("20:00", "21:00"),
            AppointmentTime.AT_21_00_22_00 => ("21:00", "22:00"),
            AppointmentTime.AT_22_00_23_00 => ("22:00", "23:00"),
            _ => ("Unknown", "Unknown")
        };
    }
}
