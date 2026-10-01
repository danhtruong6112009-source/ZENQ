import os
import json

base_api_dir = r"C:\Users\Danh\Desktop\zenq-graphics-api"
public_dir = os.path.join(base_api_dir, "public")

DECORATIVE_CATEGORIES = [
    {
        "folder": "frames",
        "name": "Khung viền & Nhãn cổ điển",
        "items": [
            ("frame-red-gold", "Khung đỏ viền vàng sang trọng", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 160" width="400" height="160">
  <defs>
    <linearGradient id="goldBorder" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#FFE082"/>
      <stop offset="50%" stop-color="#FFD700"/>
      <stop offset="100%" stop-color="#FFA000"/>
    </linearGradient>
    <linearGradient id="redFill" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#9E0B0F"/>
      <stop offset="100%" stop-color="#700508"/>
    </linearGradient>
  </defs>
  <!-- Outer red notched shape -->
  <path d="M 25 15 L 375 15 A 10 10 0 0 1 385 25 L 385 135 A 10 10 0 0 1 375 145 L 25 145 A 10 10 0 0 1 15 135 L 15 25 A 10 10 0 0 1 25 15 Z" fill="url(#redFill)" stroke="url(#goldBorder)" stroke-width="4"/>
  <!-- Inner double gold fine contour -->
  <path d="M 32 24 L 368 24 A 6 6 0 0 1 374 30 L 374 130 A 6 6 0 0 1 368 136 L 32 136 A 6 6 0 0 1 26 130 L 26 30 A 6 6 0 0 1 32 24 Z" fill="none" stroke="#FFD700" stroke-width="1.8"/>
  <!-- Classic corner notches -->
  <path d="M 38 24 L 38 36 L 26 36 M 362 24 L 362 36 L 374 36 M 38 136 L 38 124 L 26 124 M 362 136 L 362 124 L 374 124" fill="none" stroke="#FFE082" stroke-width="1.5"/>
</svg>'''),
            ("frame-vintage-rect", "Khung viền chữ nhật góc khuyết", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 240" width="400" height="240">
  <!-- Outer ornate frame with concave corners -->
  <path d="M 40 20 L 360 20 A 18 18 0 0 0 380 40 L 380 200 A 18 18 0 0 0 360 220 L 40 220 A 18 18 0 0 0 20 200 L 20 40 A 18 18 0 0 0 40 20 Z" fill="none" stroke="#1E293B" stroke-width="3"/>
  <!-- Inner fine frame -->
  <path d="M 44 28 L 356 28 A 12 12 0 0 0 372 44 L 372 196 A 12 12 0 0 0 356 212 L 44 212 A 12 12 0 0 0 28 196 L 28 44 A 12 12 0 0 0 44 28 Z" fill="none" stroke="#1E293B" stroke-width="1.2"/>
  <!-- Corner dots -->
  <circle cx="28" cy="28" r="3" fill="#1E293B"/>
  <circle cx="372" cy="28" r="3" fill="#1E293B"/>
  <circle cx="28" cy="212" r="3" fill="#1E293B"/>
  <circle cx="372" cy="212" r="3" fill="#1E293B"/>
</svg>'''),
            ("frame-vintage-vertical", "Khung viền đứng giấy khen", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 280 400" width="280" height="400">
  <!-- Vertical plaque border -->
  <path d="M 35 15 L 245 15 A 16 16 0 0 0 265 35 L 265 365 A 16 16 0 0 0 245 385 L 35 385 A 16 16 0 0 0 15 365 L 15 35 A 16 16 0 0 0 35 15 Z" fill="none" stroke="#1E293B" stroke-width="3"/>
  <path d="M 39 23 L 241 23 A 10 10 0 0 0 257 39 L 257 361 A 10 10 0 0 0 241 377 L 39 377 A 10 10 0 0 0 23 361 L 23 39 A 10 10 0 0 0 39 23 Z" fill="none" stroke="#1E293B" stroke-width="1.2"/>
  <circle cx="23" cy="23" r="3" fill="#1E293B"/>
  <circle cx="257" cy="23" r="3" fill="#1E293B"/>
  <circle cx="23" cy="377" r="3" fill="#1E293B"/>
  <circle cx="257" cy="377" r="3" fill="#1E293B"/>
</svg>'''),
            ("frame-ornate-badge", "Khung nhãn góc lượn cổ điển", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 180" width="400" height="180">
  <path d="M 60 20 C 35 20, 20 35, 20 60 C 20 75, 10 80, 10 90 C 10 100, 20 105, 20 120 C 20 145, 35 160, 60 160 L 340 160 C 365 160, 380 145, 380 120 C 380 105, 390 100, 390 90 C 390 80, 380 75, 380 60 C 380 35, 365 20, 340 20 Z" fill="none" stroke="#1E293B" stroke-width="3"/>
  <path d="M 64 28 C 42 28, 28 42, 28 64 C 28 78, 18 82, 18 90 C 18 98, 28 102, 28 116 C 28 138, 42 152, 64 152 L 336 152 C 358 152, 372 138, 372 116 C 372 102, 382 98, 382 90 C 382 82, 372 78, 372 64 C 372 42, 358 28, 336 28 Z" fill="none" stroke="#1E293B" stroke-width="1.2"/>
</svg>'''),
            ("divider-vintage-swirl", "Hoa văn phân cách cổ điển", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 500 80" width="500" height="80">
  <path d="M 20 40 Q 90 40, 130 30 T 210 35 T 240 40 T 250 40" fill="none" stroke="#1E293B" stroke-width="2"/>
  <path d="M 480 40 Q 410 40, 370 30 T 290 35 T 260 40 T 250 40" fill="none" stroke="#1E293B" stroke-width="2"/>
  <circle cx="250" cy="40" r="5" fill="#1E293B"/>
  <path d="M 235 30 Q 250 20, 265 30 Q 250 35, 235 30 Z" fill="#1E293B"/>
  <path d="M 235 50 Q 250 60, 265 50 Q 250 45, 235 50 Z" fill="#1E293B"/>
  <circle cx="215" cy="40" r="2.5" fill="#1E293B"/>
  <circle cx="285" cy="40" r="2.5" fill="#1E293B"/>
</svg>'''),
            ("divider-flourish", "Đường nét hoa văn cách điệu", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 60" width="400" height="60">
  <path d="M 20 30 L 170 30 M 230 30 L 380 30" stroke="#1E293B" stroke-width="2"/>
  <path d="M 200 12 L 208 26 L 222 26 L 211 35 L 215 48 L 200 40 L 185 48 L 189 35 L 178 26 L 192 26 Z" fill="#D4AF37" stroke="#1E293B" stroke-width="1.5"/>
</svg>'''),
            ("frame-certificate-luxury", "Khung chứng chỉ mạ vàng", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 260" width="360" height="260">
  <defs>
    <linearGradient id="goldGrad" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#FCD34D"/>
      <stop offset="50%" stop-color="#D97706"/>
      <stop offset="100%" stop-color="#F59E0B"/>
    </linearGradient>
  </defs>
  <rect x="15" y="15" width="330" height="230" rx="4" fill="none" stroke="url(#goldGrad)" stroke-width="4"/>
  <rect x="25" y="25" width="310" height="210" rx="2" fill="none" stroke="url(#goldGrad)" stroke-width="1.5"/>
  <path d="M 15 45 L 45 15 M 345 45 L 315 15 M 15 215 L 45 245 M 345 215 L 315 245" stroke="url(#goldGrad)" stroke-width="2"/>
</svg>'''),
            ("frame-floral-corners", "Khung viền góc hoa văn tây phương", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 340 240" width="340" height="240">
  <rect x="30" y="30" width="280" height="180" fill="none" stroke="#1E293B" stroke-width="1.5"/>
  <path d="M 15 40 Q 40 40, 40 15 Q 40 30, 55 30 L 285 30 Q 300 30, 300 15 Q 300 40, 325 40" fill="none" stroke="#1E293B" stroke-width="2"/>
  <path d="M 15 200 Q 40 200, 40 225 Q 40 210, 55 210 L 285 210 Q 300 210, 300 225 Q 300 200, 325 200" fill="none" stroke="#1E293B" stroke-width="2"/>
</svg>'''),
            ("frame-polaroid-shadow", "Khung ảnh nghệ thuật viền nổi", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 280 340" width="280" height="340">
  <rect x="15" y="15" width="250" height="310" rx="6" fill="#FFFFFF" stroke="#CBD5E1" stroke-width="2"/>
  <rect x="28" y="28" width="224" height="230" rx="3" fill="#F1F5F9" stroke="#E2E8F0" stroke-width="1"/>
</svg>'''),
            ("divider-laurel-wreath", "Vòng nguyệt quế vinh danh", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 100" width="300" height="100">
  <path d="M 150 85 C 90 85, 45 60, 40 25 C 50 35, 65 42, 85 45 C 65 52, 50 65, 50 78 C 75 75, 110 82, 145 85 Z" fill="#D97706"/>
  <path d="M 150 85 C 210 85, 255 60, 260 25 C 250 35, 235 42, 215 45 C 235 52, 250 65, 250 78 C 225 75, 190 82, 155 85 Z" fill="#D97706"/>
  <circle cx="150" cy="85" r="4" fill="#D97706"/>
</svg>'''),
            ("frame-classic-double", "Khung viền đôi thanh lịch", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 380 240" width="380" height="240">
  <rect x="15" y="15" width="350" height="210" rx="4" fill="none" stroke="#1E293B" stroke-width="3"/>
  <rect x="23" y="23" width="334" height="194" rx="2" fill="none" stroke="#1E293B" stroke-width="1"/>
</svg>'''),
            ("frame-art-deco", "Khung góc phong cách Art Deco", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 240" width="360" height="240">
  <rect x="20" y="20" width="320" height="200" fill="none" stroke="#D4AF37" stroke-width="2"/>
  <path d="M 10 35 L 35 10 M 10 45 L 45 10 M 350 35 L 325 10 M 350 45 L 315 10" stroke="#D4AF37" stroke-width="1.8"/>
  <path d="M 10 205 L 35 230 M 10 195 L 45 230 M 350 205 L 325 230 M 350 195 L 315 230" stroke="#D4AF37" stroke-width="1.8"/>
</svg>''')
        ]
    },
    {
        "folder": "waves_effects",
        "name": "Sóng nghệ thuật & Làn khói",
        "items": [
            ("smoke-golden-swirl", "Dải khói vàng hoàng kim", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 500 240" width="500" height="240">
  <defs>
    <linearGradient id="goldSmoke1" x1="0" y1="0" x2="1" y2="0.8">
      <stop offset="0%" stop-color="#FFE082" stop-opacity="0.9"/>
      <stop offset="40%" stop-color="#FFCA28" stop-opacity="0.8"/>
      <stop offset="80%" stop-color="#FFB300" stop-opacity="0.6"/>
      <stop offset="100%" stop-color="#FFA000" stop-opacity="0.1"/>
    </linearGradient>
    <linearGradient id="goldSmoke2" x1="0" y1="1" x2="1" y2="0">
      <stop offset="0%" stop-color="#FFF8E1" stop-opacity="0.8"/>
      <stop offset="50%" stop-color="#FFD54F" stop-opacity="0.6"/>
      <stop offset="100%" stop-color="#FF8F00" stop-opacity="0.0"/>
    </linearGradient>
  </defs>
  <!-- Primary golden smoke cloud body -->
  <path d="M 40 180 C 100 190, 150 160, 200 170 C 260 180, 290 140, 340 130 C 400 120, 440 90, 480 60 C 460 70, 420 85, 380 90 C 330 95, 300 120, 240 115 C 190 110, 150 130, 90 120 C 50 115, 30 140, 40 180 Z" fill="url(#goldSmoke1)"/>
  <!-- Secondary ethereal wisps -->
  <path d="M 90 170 C 160 160, 210 135, 270 145 C 330 155, 370 125, 420 100 C 450 85, 470 70, 485 55 C 470 65, 435 80, 395 90 C 340 105, 290 125, 220 120 C 160 115, 120 145, 90 170 Z" fill="url(#goldSmoke2)"/>
</svg>'''),
            ("smoke-golden-flow", "Làn khói vàng uốn lượn", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 450 200" width="450" height="200">
  <defs>
    <linearGradient id="smokeFlow" x1="0" y1="0.5" x2="1" y2="0.5">
      <stop offset="0%" stop-color="#FFD54F" stop-opacity="0.9"/>
      <stop offset="50%" stop-color="#FFB300" stop-opacity="0.7"/>
      <stop offset="100%" stop-color="#FF8F00" stop-opacity="0.1"/>
    </linearGradient>
  </defs>
  <path d="M 30 130 Q 110 150, 170 120 T 310 110 T 430 50 Q 370 75, 300 85 T 160 95 T 30 130 Z" fill="url(#smokeFlow)"/>
</svg>'''),
            ("wave-abstract-blue", "Dải sóng vector xanh nghệ thuật", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 320" width="480" height="320">
  <!-- Dynamic glowing blue wave ribbon -->
  <path d="M 30 160 C 100 50, 200 40, 260 110 C 320 180, 240 260, 310 290 C 380 320, 440 270, 460 210" fill="none" stroke="#38BDF8" stroke-width="2.5"/>
  <path d="M 35 168 C 105 60, 202 52, 260 118 C 318 184, 242 258, 312 284 C 378 310, 435 264, 455 208" fill="none" stroke="#0284C7" stroke-width="2"/>
  <path d="M 40 176 C 110 70, 204 64, 260 126 C 316 188, 244 256, 314 278 C 376 300, 430 258, 450 206" fill="none" stroke="#60A5FA" stroke-width="1.8"/>
  <path d="M 45 184 C 115 80, 206 76, 260 134 C 314 192, 246 254, 316 272 C 374 290, 425 252, 445 204" fill="none" stroke="#818CF8" stroke-width="1.5"/>
  <path d="M 50 192 C 120 90, 208 88, 260 142 C 312 196, 248 252, 318 266 C 372 280, 420 246, 440 202" fill="none" stroke="#38BDF8" stroke-width="1.2"/>
  <path d="M 55 200 C 125 100, 210 100, 260 150 C 310 200, 250 250, 320 260 C 370 270, 415 240, 435 200" fill="none" stroke="#0284C7" stroke-width="1"/>
</svg>'''),
            ("wave-flowing-cyan", "Dải lụa vector phát sáng", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 500 200" width="500" height="200">
  <defs>
    <linearGradient id="cyanSilk" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#06B6D4" stop-opacity="0.8"/>
      <stop offset="50%" stop-color="#3B82F6" stop-opacity="0.6"/>
      <stop offset="100%" stop-color="#8B5CF6" stop-opacity="0.2"/>
    </linearGradient>
  </defs>
  <path d="M 10 120 C 120 40, 220 180, 340 100 C 410 50, 460 80, 490 120 C 440 140, 380 90, 300 130 C 200 180, 100 80, 10 120 Z" fill="url(#cyanSilk)"/>
</svg>'''),
            ("light-beam-glow", "Tia sáng lung linh phát quang", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 400" width="400" height="400">
  <defs>
    <radialGradient id="lightGlow" cx="0.5" cy="0.5" r="0.5">
      <stop offset="0%" stop-color="#FFFFFF" stop-opacity="1"/>
      <stop offset="25%" stop-color="#FEF08A" stop-opacity="0.8"/>
      <stop offset="60%" stop-color="#F59E0B" stop-opacity="0.3"/>
      <stop offset="100%" stop-color="#D97706" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <circle cx="200" cy="200" r="180" fill="url(#lightGlow)"/>
  <line x1="20" y1="200" x2="380" y2="200" stroke="#FFFBEB" stroke-width="2"/>
  <line x1="200" y1="20" x2="200" y2="380" stroke="#FFFBEB" stroke-width="2"/>
  <line x1="70" y1="70" x2="330" y2="330" stroke="#FEF08A" stroke-width="1.2"/>
  <line x1="70" y1="330" x2="330" y2="70" stroke="#FEF08A" stroke-width="1.2"/>
</svg>'''),
            ("sparkle-starburst-gold", "Ngôi sao tỏa sáng hoàng kim", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" width="300" height="300">
  <defs>
    <linearGradient id="starGold" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#FDE047"/>
      <stop offset="100%" stop-color="#CA8A04"/>
    </linearGradient>
  </defs>
  <path d="M 150 10 Q 150 150, 10 150 Q 150 150, 150 290 Q 150 150, 290 150 Q 150 150, 150 10 Z" fill="url(#starGold)"/>
  <path d="M 150 60 Q 150 150, 60 150 Q 150 150, 150 240 Q 150 150, 240 150 Q 150 150, 150 60 Z" fill="#FEF9C3"/>
</svg>'''),
            ("wave-mesh-gradient", "Lưới sóng đa sắc hiện đại", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 450 220" width="450" height="220">
  <path d="M 20 80 Q 120 180, 220 80 T 420 120" fill="none" stroke="#8B5CF6" stroke-width="2"/>
  <path d="M 20 95 Q 120 195, 220 95 T 420 135" fill="none" stroke="#6366F1" stroke-width="2"/>
  <path d="M 20 110 Q 120 210, 220 110 T 420 150" fill="none" stroke="#3B82F6" stroke-width="2"/>
  <path d="M 20 125 Q 120 225, 220 125 T 420 165" fill="none" stroke="#06B6D4" stroke-width="2"/>
</svg>'''),
            ("sunburst-vintage", "Tia sáng mặt trời tỏa rạng", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" width="300" height="300">
  <g stroke="#F59E0B" stroke-width="2">
    <line x1="150" y1="10" x2="150" y2="70"/>
    <line x1="150" y1="230" x2="150" y2="290"/>
    <line x1="10" y1="150" x2="70" y2="150"/>
    <line x1="230" y1="150" x2="290" y2="150"/>
    <line x1="50" y1="50" x2="95" y2="95"/>
    <line x1="205" y1="205" x2="250" y2="250"/>
    <line x1="50" y1="250" x2="95" y2="205"/>
    <line x1="205" y1="95" x2="250" y2="50"/>
  </g>
  <circle cx="150" cy="150" r="30" fill="#FEF3C7" stroke="#F59E0B" stroke-width="3"/>
</svg>''')
        ]
    },
    {
        "folder": "gradients",
        "name": "Chuyển sắc & Phông nền",
        "items": [
            ("gradient-dark-fade", "Chuyển sắc đen mờ dần", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" width="300" height="300">
  <defs>
    <linearGradient id="darkFade" x1="0" y1="1" x2="0" y2="0">
      <stop offset="0%" stop-color="#050505" stop-opacity="1"/>
      <stop offset="50%" stop-color="#18181B" stop-opacity="0.95"/>
      <stop offset="100%" stop-color="#27272A" stop-opacity="0.6"/>
    </linearGradient>
  </defs>
  <rect x="0" y="0" width="300" height="300" rx="8" fill="url(#darkFade)"/>
</svg>'''),
            ("gradient-vignette-black", "Khung bóng đổ viền mờ tối", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" width="300" height="300">
  <defs>
    <radialGradient id="vignette" cx="0.5" cy="0.5" r="0.6">
      <stop offset="20%" stop-color="#27272A" stop-opacity="0.2"/>
      <stop offset="70%" stop-color="#18181B" stop-opacity="0.85"/>
      <stop offset="100%" stop-color="#09090B" stop-opacity="1"/>
    </radialGradient>
  </defs>
  <rect x="0" y="0" width="300" height="300" rx="8" fill="url(#vignette)"/>
</svg>'''),
            ("gradient-soft-linear", "Chuyển sắc xám trắng mềm mại", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" width="300" height="300">
  <defs>
    <linearGradient id="softGray" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#334155" stop-opacity="1"/>
      <stop offset="60%" stop-color="#94A3B8" stop-opacity="0.85"/>
      <stop offset="100%" stop-color="#F8FAFC" stop-opacity="1"/>
    </linearGradient>
  </defs>
  <rect x="0" y="0" width="300" height="300" rx="8" fill="url(#softGray)"/>
</svg>'''),
            ("gradient-radial-glow", "Vòng sáng tỏa tâm radial", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" width="300" height="300">
  <defs>
    <radialGradient id="radialGlow" cx="0.5" cy="0.5" r="0.5">
      <stop offset="0%" stop-color="#38BDF8" stop-opacity="0.9"/>
      <stop offset="60%" stop-color="#0284C7" stop-opacity="0.4"/>
      <stop offset="100%" stop-color="#0F172A" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <rect x="0" y="0" width="300" height="300" rx="8" fill="url(#radialGlow)"/>
</svg>'''),
            ("gradient-sunset-warm", "Chuyển sắc hoàng hôn ấm áp", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 300" width="300" height="300">
  <defs>
    <linearGradient id="warmSunset" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#F43F5E"/>
      <stop offset="50%" stop-color="#FB923C"/>
      <stop offset="100%" stop-color="#FBBF24"/>
    </linearGradient>
  </defs>
  <rect x="0" y="0" width="300" height="300" rx="8" fill="url(#warmSunset)"/>
</svg>'''),
            ("gradient-bottom-shadow", "Bóng đổ chân trang tối", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 150" width="400" height="150">
  <defs>
    <linearGradient id="botShadow" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#000000" stop-opacity="0"/>
      <stop offset="60%" stop-color="#000000" stop-opacity="0.6"/>
      <stop offset="100%" stop-color="#000000" stop-opacity="0.95"/>
    </linearGradient>
  </defs>
  <rect x="0" y="0" width="400" height="150" fill="url(#botShadow)"/>
</svg>''')
        ]
    },
    {
        "folder": "artistic_floral",
        "name": "Hoa & Nghệ thuật minh họa",
        "items": [
            ("flower-glowing-lily", "Bông hoa bách hợp phát sáng", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 360" width="320" height="360">
  <defs>
    <radialGradient id="auraGlow" cx="0.5" cy="0.45" r="0.45">
      <stop offset="0%" stop-color="#93C5FD" stop-opacity="0.5"/>
      <stop offset="60%" stop-color="#60A5FA" stop-opacity="0.2"/>
      <stop offset="100%" stop-color="#1E3A8A" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="petalPink" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#FDA4AF"/>
      <stop offset="50%" stop-color="#F43F5E"/>
      <stop offset="100%" stop-color="#FFF1F2"/>
    </linearGradient>
    <linearGradient id="petalYellow" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#FEF08A"/>
      <stop offset="100%" stop-color="#FB923C"/>
    </linearGradient>
  </defs>
  <!-- Background glowing aura -->
  <circle cx="160" cy="160" r="140" fill="url(#auraGlow)"/>
  <!-- Stem & Leaves -->
  <path d="M 160 200 Q 155 270, 150 340" fill="none" stroke="#10B981" stroke-width="5" stroke-linecap="round"/>
  <path d="M 155 260 Q 120 250, 100 270 Q 130 280, 153 275 Z" fill="#059669"/>
  <path d="M 153 290 Q 185 285, 205 305 Q 180 315, 150 305 Z" fill="#059669"/>
  <!-- Blooming Lily Petals -->
  <!-- Back petal top left -->
  <path d="M 160 160 C 130 110, 90 70, 75 75 C 65 85, 95 130, 160 160 Z" fill="#F472B6"/>
  <!-- Top petal -->
  <path d="M 160 160 C 145 90, 150 40, 165 40 C 180 40, 185 90, 160 160 Z" fill="url(#petalPink)"/>
  <!-- Back petal top right -->
  <path d="M 160 160 C 190 110, 230 70, 245 75 C 255 85, 225 130, 160 160 Z" fill="url(#petalPink)"/>
  <!-- Bottom left petal -->
  <path d="M 160 160 C 110 165, 60 180, 65 200 C 75 210, 120 190, 160 160 Z" fill="url(#petalYellow)"/>
  <!-- Bottom right petal -->
  <path d="M 160 160 C 210 165, 260 180, 255 200 C 245 210, 200 190, 160 160 Z" fill="url(#petalYellow)"/>
  <!-- Front center petal -->
  <path d="M 160 160 C 140 190, 145 235, 160 240 C 175 235, 180 190, 160 160 Z" fill="url(#petalPink)"/>
  <!-- Stamens with golden pollen -->
  <line x1="160" y1="160" x2="135" y2="115" stroke="#FBBF24" stroke-width="2"/>
  <circle cx="135" cy="115" r="4" fill="#D97706"/>
  <line x1="160" y1="160" x2="160" y2="105" stroke="#FBBF24" stroke-width="2"/>
  <circle cx="160" cy="105" r="4" fill="#D97706"/>
  <line x1="160" y1="160" x2="185" y2="115" stroke="#FBBF24" stroke-width="2"/>
  <circle cx="185" cy="115" r="4" fill="#D97706"/>
</svg>'''),
            ("flower-blooming-lotus", "Hoa sen nở rực rỡ", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 240" width="320" height="240">
  <defs>
    <linearGradient id="lotusPink" x1="0" y1="1" x2="0" y2="0">
      <stop offset="0%" stop-color="#F43F5E"/>
      <stop offset="100%" stop-color="#FCE7F3"/>
    </linearGradient>
  </defs>
  <!-- Base leaves -->
  <ellipse cx="160" cy="205" rx="130" ry="18" fill="#047857"/>
  <!-- Lotus petals -->
  <path d="M 160 180 C 110 170, 70 140, 60 120 C 80 115, 120 145, 160 180 Z" fill="#FB7185"/>
  <path d="M 160 180 C 210 170, 250 140, 260 120 C 240 115, 200 145, 160 180 Z" fill="#FB7185"/>
  <path d="M 160 180 C 125 150, 95 100, 105 75 C 125 75, 145 125, 160 180 Z" fill="url(#lotusPink)"/>
  <path d="M 160 180 C 195 150, 225 100, 215 75 C 195 75, 175 125, 160 180 Z" fill="url(#lotusPink)"/>
  <path d="M 160 180 C 140 120, 140 60, 160 40 C 180 60, 180 120, 160 180 Z" fill="url(#lotusPink)"/>
</svg>'''),
            ("botanical-gold-branch", "Cành lá vàng ánh kim", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 260 360" width="260" height="360">
  <defs>
    <linearGradient id="branchGold" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#FDE047"/>
      <stop offset="50%" stop-color="#D97706"/>
      <stop offset="100%" stop-color="#78350F"/>
    </linearGradient>
  </defs>
  <path d="M 50 330 Q 110 220, 180 40" fill="none" stroke="url(#branchGold)" stroke-width="3" stroke-linecap="round"/>
  <!-- Leaf pairs -->
  <path d="M 175 55 Q 215 45, 225 25 Q 195 30, 175 55 Z" fill="url(#branchGold)"/>
  <path d="M 155 90 Q 115 75, 110 50 Q 135 65, 155 90 Z" fill="url(#branchGold)"/>
  <path d="M 145 125 Q 190 120, 205 95 Q 170 105, 145 125 Z" fill="url(#branchGold)"/>
  <path d="M 125 160 Q 80 150, 75 125 Q 105 135, 125 160 Z" fill="url(#branchGold)"/>
  <path d="M 110 200 Q 160 195, 175 170 Q 135 180, 110 200 Z" fill="url(#branchGold)"/>
  <path d="M 90 240 Q 45 230, 40 205 Q 70 215, 90 240 Z" fill="url(#branchGold)"/>
</svg>'''),
            ("flower-vintage-rose", "Hoa hồng cổ điển", '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 280 280" width="280" height="280">
  <path d="M 140 140 C 130 110, 160 110, 150 140 C 165 130, 175 150, 155 160 C 150 175, 125 170, 130 150 Z" fill="#E11D48"/>
  <path d="M 140 120 C 110 90, 170 80, 180 120 C 200 135, 185 180, 155 185 C 120 190, 95 155, 110 130 Z" fill="none" stroke="#BE123C" stroke-width="2.5"/>
  <path d="M 140 100 C 80 70, 190 50, 210 110 C 230 150, 190 210, 140 210 C 80 205, 70 130, 100 105 Z" fill="none" stroke="#9F1239" stroke-width="2"/>
</svg>''')
        ]
    }
]

# Write SVGs and update catalog
new_cat_entries = []

for cat in DECORATIVE_CATEGORIES:
    folder = cat["folder"]
    title = cat["name"]
    target_dir = os.path.join(public_dir, folder)
    os.makedirs(target_dir, exist_ok=True)

    items = []
    for item_id, item_name, svg_code in cat["items"]:
        file_path = os.path.join(target_dir, f"{item_id}.svg")
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(svg_code.strip())

        items.append({
            "id": f"{folder}_{item_id.replace('-', '_')}",
            "name": item_name,
            "url": f"https://zenq-graphics-api-online.vercel.app/{folder}/{item_id}.svg",
            "type": "svg"
        })
    print(f"Created {len(items)} SVGs in public/{folder}/")
    new_cat_entries.append({
        "name": title,
        "items": items
    })

# Load existing graphics.json and merge
json_path = os.path.join(base_api_dir, "data", "graphics.json")
with open(json_path, "r", encoding="utf-8") as f:
    existing_data = json.load(f)

# Keep existing 10 categories, append or replace new categories
existing_categories = [c for c in existing_data.get("categories", []) if c["name"] not in [x["name"] for x in new_cat_entries]]
all_categories = existing_categories + new_cat_entries

total_count = sum(len(c["items"]) for c in all_categories)
catalog_data = {
    "version": "2.1",
    "total": total_count,
    "categories": all_categories
}

with open(json_path, "w", encoding="utf-8") as f:
    json.dump(catalog_data, f, ensure_ascii=False, indent=2)

print(f"Updated graphics.json: Total categories = {len(all_categories)}, Total items = {total_count}")
