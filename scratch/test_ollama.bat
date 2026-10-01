@echo off
curl -s -X POST http://localhost:11434/v1/chat/completions -H "Content-Type: application/json" -d "{\"model\":\"deepseek-r1:7b\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}],\"temperature\":0.7}" --max-time 120
