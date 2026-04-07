Xin chào tất cả các bạn!

Cảm ơn vì đã chú ý đến dự án của chúng tôi

nó để làm gì thì ai cũng biết mà nhỉ?

ta đến phần cách chạy luôn



B1: cài đặt api

mở cmd tại vị trí V-Shield\\API\\API\\API

chạy

dotnet ef database drop -f

rmdir /s /q "Migrations"

dotnet ef migrations add InitialCreate

dotnet ef database update

dotnet run --launch-profile "https"



B2: cài view

mở cmd tại vị trí V-Shield\\View

chạy

npm install

npm run dev



B3: cài AI đọc biển

mở cmd tại vị trí V-Shield\\AI\_Project\\doc\_bien\_gpu

chạy

python -m venv venv

venv\\Scripts\\activate

pip install -r requirements.txt

python docbien.py



B4: cài đọc QR động

mở cmd tại vị trí V-Shield\\AI\_Project\\QR\_Dong

chạy

python -m venv venv

venv\\Scripts\\activate

pip install -r requirements.txt

python QR\_Dong.py



B5: cài camera

mở cmd

chạy

cloudflared tunnel login

cloudflared tunnel create cam-tunnel

cloudflared tunnel route dns cam-tunnel cam.maiai06.site



chú ý login bằng phamvanthanh2734@gmail.com

















phần mã phòng trường hợp cần chạy thủ công (chỉ có bậc đại đế chân chính mới có thể sử dụng được)





pip freeze > requirements.txt



set PORT=5002

python faceid\_single\_read\_lock.py



pip cache purge

pip install pyodbc

pip install flask flask-cors



cho view

npm install @ffmpeg-installer/ffmpeg

http://localhost:1984



cho cam

winget install ffmpeg

cam\_gia\_lap.py

