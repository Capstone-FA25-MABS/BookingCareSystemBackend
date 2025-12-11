using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Utils.Email;

/// <summary>
/// Email templates for nutrition notifications
/// </summary>
public static class NutritionEmailTemplate
{
    public static string BuildMealPlanEmail(DailyMealPlanGeneratedEvent data)
    {
        var appUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "https://bookingcare.com";
        
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                   color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .header p {{ margin: 10px 0 0 0; font-size: 16px; opacity: 0.9; }}
        .content {{ background: #f9f9f9; padding: 30px; }}
        .nutrition-card {{ background: white; padding: 20px; margin: 15px 0; 
                          border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .nutrition-card h2 {{ margin-top: 0; color: #667eea; }}
        .nutrition-grid {{ display: table; width: 100%; text-align: center; }}
        .nutrition-item {{ display: table-cell; padding: 10px; }}
        .nutrition-value {{ font-size: 24px; font-weight: bold; color: #667eea; }}
        .nutrition-label {{ font-size: 14px; color: #666; margin-top: 5px; }}
        .cta-button {{ display: inline-block; background: #667eea; color: white !important; 
                      padding: 15px 30px; text-decoration: none; border-radius: 5px; 
                      margin: 20px 0; font-weight: bold; }}
        .cta-button:hover {{ background: #5568d3; }}
        .tips {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
        .tips h3 {{ margin-top: 0; color: #856404; }}
        .tips ul {{ margin: 10px 0; padding-left: 20px; }}
        .tips li {{ margin: 5px 0; color: #856404; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Thực đơn dinh dưỡng hôm nay</h1>
            <p>{data.Date:dddd, dd/MM/yyyy}</p>
        </div>
        
        <div class='content'>
            <div class='nutrition-card'>
                <h2>📊 Tổng quan dinh dưỡng</h2>
                <div class='nutrition-grid'>
                    <div class='nutrition-item'>
                        <div class='nutrition-value'>{data.TotalCalories}</div>
                        <div class='nutrition-label'>Calories (kcal)</div>
                    </div>
                    <div class='nutrition-item'>
                        <div class='nutrition-value'>{data.TotalProteinG}g</div>
                        <div class='nutrition-label'>Protein</div>
                    </div>
                    <div class='nutrition-item'>
                        <div class='nutrition-value'>{data.TotalCarbsG}g</div>
                        <div class='nutrition-label'>Carbs</div>
                    </div>
                    <div class='nutrition-item'>
                        <div class='nutrition-value'>{data.TotalFatG}g</div>
                        <div class='nutrition-label'>Fat</div>
                    </div>
                </div>
            </div>
            
            <div class='nutrition-card'>
                <h2>🍴 Bữa ăn hôm nay</h2>
                <p>Thực đơn của bạn bao gồm <strong>{data.MealCount} bữa ăn</strong> được thiết kế đặc biệt 
                   dựa trên mục tiêu sức khỏe và sở thích của bạn.</p>
                <p>Xem chi tiết món ăn và thông tin dinh dưỡng trong ứng dụng.</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='{appUrl}/nutrition/meal-plans/{data.Date:yyyy-MM-dd}' class='cta-button'>
                    Xem thực đơn chi tiết
                </a>
            </div>
            
            <div class='tips'>
                <h3>💡 Lưu ý quan trọng</h3>
                <ul>
                    <li>Uống đủ 2-2.5 lít nước mỗi ngày</li>
                    <li>Ăn đúng giờ và không bỏ bữa</li>
                    <li>Kết hợp với kế hoạch tập luyện để đạt hiệu quả tốt nhất</li>
                    <li>Điều chỉnh khẩu phần theo cảm giác đói no của cơ thể</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>BookingCare</strong> - Hệ thống chăm sóc sức khỏe thông minh</p>
            <p>Email này được gửi tự động từ hệ thống</p>
            <p>Nếu bạn không muốn nhận email này, vui lòng cập nhật cài đặt trong ứng dụng</p>
        </div>
    </div>
</body>
</html>
";
    }
    
    public static string BuildWorkoutPlanEmail(DailyWorkoutPlanGeneratedEvent data)
    {
        var appUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "https://bookingcare.com";
        
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); 
                   color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .header p {{ margin: 10px 0 0 0; font-size: 16px; opacity: 0.9; }}
        .content {{ background: #f9f9f9; padding: 30px; }}
        .workout-card {{ background: white; padding: 20px; margin: 15px 0; 
                        border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .workout-card h2 {{ margin-top: 0; color: #f5576c; }}
        .workout-grid {{ display: table; width: 100%; text-align: center; }}
        .workout-item {{ display: table-cell; padding: 10px; }}
        .workout-value {{ font-size: 24px; font-weight: bold; color: #f5576c; }}
        .workout-label {{ font-size: 14px; color: #666; margin-top: 5px; }}
        .cta-button {{ display: inline-block; background: #f5576c; color: white !important; 
                      padding: 15px 30px; text-decoration: none; border-radius: 5px; 
                      margin: 20px 0; font-weight: bold; }}
        .cta-button:hover {{ background: #e04858; }}
        .tips {{ background: #d1ecf1; border-left: 4px solid #17a2b8; padding: 15px; margin: 15px 0; }}
        .tips h3 {{ margin-top: 0; color: #0c5460; }}
        .tips ul {{ margin: 10px 0; padding-left: 20px; }}
        .tips li {{ margin: 5px 0; color: #0c5460; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💪 Kế hoạch tập luyện hôm nay</h1>
            <p>{data.Date:dddd, dd/MM/yyyy}</p>
        </div>
        
        <div class='content'>
            <div class='workout-card'>
                <h2>🏋️ Thông tin tập luyện</h2>
                <div class='workout-grid'>
                    <div class='workout-item'>
                        <div class='workout-value'>{data.WorkoutType}</div>
                        <div class='workout-label'>Loại hình</div>
                    </div>
                    <div class='workout-item'>
                        <div class='workout-value'>{data.DurationMinutes}</div>
                        <div class='workout-label'>Phút</div>
                    </div>
                    <div class='workout-item'>
                        <div class='workout-value'>{data.ExerciseCount}</div>
                        <div class='workout-label'>Bài tập</div>
                    </div>
                    <div class='workout-item'>
                        <div class='workout-value'>{data.EstimatedCaloriesBurned}</div>
                        <div class='workout-label'>Calories đốt</div>
                    </div>
                </div>
            </div>
            
            <div class='workout-card'>
                <h2>🎯 Mục tiêu hôm nay</h2>
                <p>Kế hoạch tập luyện <strong>{data.WorkoutType}</strong> được thiết kế riêng cho bạn 
                   với <strong>{data.ExerciseCount} bài tập</strong> trong <strong>{data.DurationMinutes} phút</strong>.</p>
                <p>Hoàn thành buổi tập này để đốt cháy <strong>{data.EstimatedCaloriesBurned} calories</strong>!</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='{appUrl}/nutrition/workout-plans/{data.Date:yyyy-MM-dd}' class='cta-button'>
                    Xem kế hoạch chi tiết
                </a>
            </div>
            
            <div class='tips'>
                <h3>💡 Lưu ý khi tập luyện</h3>
                <ul>
                    <li>Khởi động 5-10 phút trước khi tập</li>
                    <li>Giữ đúng tư thế để tránh chấn thương</li>
                    <li>Nghỉ ngơi 30-60 giây giữa các sets</li>
                    <li>Uống nước đủ trong và sau khi tập</li>
                    <li>Kết thúc với stretching 5-10 phút</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>BookingCare</strong> - Hệ thống chăm sóc sức khỏe thông minh</p>
            <p>Email này được gửi tự động từ hệ thống</p>
            <p>Nếu bạn không muốn nhận email này, vui lòng cập nhật cài đặt trong ứng dụng</p>
        </div>
    </div>
</body>
</html>
";
    }
}
