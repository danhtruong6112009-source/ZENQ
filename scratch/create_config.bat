@echo off
echo Creating ai_config.xml with Ollama profile...
set "CONFIG_DIR=%APPDATA%\GenOffice"
if not exist "%CONFIG_DIR%" mkdir "%CONFIG_DIR%"
(
echo ^<?xml version="1.0" encoding="utf-8"?^>
echo ^<AiConfig^>
echo   ^<ActiveProfileId^>ollama-local^</ActiveProfileId^>
echo   ^<SystemInstructions^>Bạn là Trợ lý AI GenOffice - Trợ lý trí tuệ nhân tạo toàn năng tích hợp trong bộ ứng dụng văn phòng GenOffice. Hãy thực hiện chính xác các yêu cầu của người dùng, trả lời súc tích, văn phong chuyên nghiệp, định dạng rõ ràng.^</SystemInstructions^>
echo   ^<Profiles^>
echo     ^<Profile^>
echo       ^<Id^>ollama-local^</Id^>
echo       ^<Name^>Ollama Local^</Name^>
echo       ^<Provider^>Ollama^</Provider^>
echo       ^<ApiKey /^>
echo       ^<SelectedModel^>deepseek-r1:7b^</SelectedModel^>
echo       ^<SavedModels^>
echo         ^<Model^>deepseek-r1:7b^</Model^>
echo       ^</SavedModels^>
echo       ^<CustomEndpoint /^>
echo       ^<Temperature^>0.7^</Temperature^>
echo     ^</Profile^>
echo   ^</Profiles^>
echo ^</AiConfig^>
) > "%CONFIG_DIR%\ai_config.xml"
echo Done! Config saved at %CONFIG_DIR%\ai_config.xml
type "%CONFIG_DIR%\ai_config.xml"
