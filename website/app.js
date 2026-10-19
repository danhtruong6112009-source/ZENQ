/**
 * ZENQ OFFICE - OFFICIAL INTERACTION SCRIPT (GOOGLE WORKSPACE AESTHETIC)
 * Bilingual Engine, Dynamic Hero Typewriter, Synchronized App Suite Switcher,
 * Interactive Live Ribbon Preview, and Accordion Micro-interactions.
 */

(function () {
  'use strict';

  // Global State
  let currentLang = localStorage.getItem('zenq_lang') || 'vi';

  // DOM Elements
  const langToggleBtn = document.getElementById('langToggleBtn');
  const navLinks = document.querySelectorAll('.nav-link');
  const navbar = document.getElementById('navbar');
  const appTabs = document.querySelectorAll('.app-tab-btn');
  const appPanels = document.querySelectorAll('.app-tab-content-panel');
  const faqFilterBtns = document.querySelectorAll('.faq-filter-btn');
  const faqItems = document.querySelectorAll('.faq-item');
  const mobileToggle = document.getElementById('mobileMenuToggle');
  const typewriterElement = document.getElementById('typewriterText');

  // Mockup elements
  const mockupAppIcon = document.getElementById('mockupAppIcon');
  const mockupDocTitle = document.getElementById('mockupDocTitle');
  const mockupSheet = document.getElementById('mockupSheet');

  // =========================================================================
  // 1. HERO DYNAMIC TYPEWRITER EFFECT
  // =========================================================================
  const typewriterPhrases = {
    vi: [
      "Soạn thảo văn bản chuẩn in ấn",
      "Bảng tính thông minh, phân tích mạnh mẽ",
      "Trình chiếu bài giảng & thuyết trình chuyên nghiệp",
      "Đọc & xử lý tài liệu PDF bảo mật",
      "ZenQ Mobile — Văn phòng di động thuần C# siêu mượt",
      "Bảo mật ngoại tuyến 100% an toàn"
    ],
    en: [
      "Professional document authoring",
      "Smart spreadsheets & powerful analysis",
      "Captivating & dynamic slide presentations",
      "Secure local PDF document reader",
      "ZenQ Mobile — High-performance native mobile suite",
      "100% Offline & Private on Windows and Android"
    ]
  };

  let typePhraseIdx = 0;
  let typeCharIdx = 0;
  let isDeleting = false;
  let typeTimer = null;

  function runTypewriter() {
    if (!typewriterElement) return;

    const phrases = typewriterPhrases[currentLang] || typewriterPhrases.vi;
    const currentPhrase = phrases[typePhraseIdx % phrases.length];

    if (isDeleting) {
      typewriterElement.textContent = currentPhrase.substring(0, typeCharIdx - 1);
      typeCharIdx--;
    } else {
      typewriterElement.textContent = currentPhrase.substring(0, typeCharIdx + 1);
      typeCharIdx++;
    }

    let delay = isDeleting ? 38 : 75;

    if (!isDeleting && typeCharIdx === currentPhrase.length) {
      delay = 2200; // Pause at full phrase
      isDeleting = true;
    } else if (isDeleting && typeCharIdx === 0) {
      isDeleting = false;
      typePhraseIdx = (typePhraseIdx + 1) % phrases.length;
      delay = 450;
    }

    typeTimer = setTimeout(runTypewriter, delay);
  }

  // =========================================================================
  // 2. BILINGUAL TRANSLATION ENGINE (Vietnamese & English Dictionary)
  // =========================================================================
  function applyLanguage(lang) {
    if (typeof DICTIONARY === 'undefined' || !DICTIONARY[lang]) lang = 'vi';
    currentLang = lang;
    localStorage.setItem('zenq_lang', lang);

    document.documentElement.setAttribute('lang', lang);

    // Update all elements with data-i18n
    const elements = document.querySelectorAll('[data-i18n]');
    elements.forEach((el) => {
      const key = el.getAttribute('data-i18n');
      if (DICTIONARY[lang] && DICTIONARY[lang][key]) {
        if (el.tagName === 'INPUT' && el.getAttribute('type') === 'text') {
          el.placeholder = DICTIONARY[lang][key];
        } else {
          el.innerHTML = DICTIONARY[lang][key];
        }
      }
    });

    // Update Language Toggle Button Text & Flag
    if (langToggleBtn) {
      const btnText = langToggleBtn.querySelector('.lang-btn-text');
      const flagIcon = langToggleBtn.querySelector('.lang-flag');
      if (btnText) {
        btnText.textContent = lang === 'vi' ? 'English' : 'Tiếng Việt';
      }
      if (flagIcon) {
        flagIcon.textContent = lang === 'vi' ? '🇺🇸' : '🇻🇳';
      }
    }

    // Reset typewriter with new language
    clearTimeout(typeTimer);
    typePhraseIdx = 0;
    typeCharIdx = 0;
    isDeleting = false;
    runTypewriter();
  }

  if (langToggleBtn) {
    langToggleBtn.addEventListener('click', () => {
      const newLang = currentLang === 'vi' ? 'en' : 'vi';
      applyLanguage(newLang);
    });
  }

  // =========================================================================
  // 3. SYNCHRONIZED APP SUITE SWITCHER (4 Core Apps Only)
  // =========================================================================
  const appMockupConfig = {
    writer: {
      title: "Bao_Cao_Chien_Luoc_Q4.docx — ZenQ Writer",
      icon: "icon/writer.png",
      html: `
        <div class="mockup-text-line title"></div>
        <div class="mockup-text-line w90"></div>
        <div class="mockup-text-line w80"></div>
        <div class="mockup-text-line w90"></div>
        <div class="mockup-text-line w70"></div>
        <div style="height: 12px;"></div>
        <div class="mockup-text-line w80"></div>
        <div class="mockup-text-line w50"></div>
      `
    },
    calc: {
      title: "Bang_Ke_Toan_Tong_Hop.xlsx — ZenQ Calc",
      icon: "icon/calc.png",
      html: `
        <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 6px; font-family: var(--font-mono); font-size: 0.8rem;">
          <div style="background: #e8f0fe; padding: 6px; font-weight: 700; color: #1a73e8;">Mã NV</div>
          <div style="background: #e8f0fe; padding: 6px; font-weight: 700; color: #1a73e8;">Họ và Tên</div>
          <div style="background: #e8f0fe; padding: 6px; font-weight: 700; color: #1a73e8;">Lương Cơ Bản</div>
          <div style="background: #e8f0fe; padding: 6px; font-weight: 700; color: #1a73e8;">Thực Nhận</div>
          <div style="background: #f8f9fa; padding: 6px;">EMP-01</div>
          <div style="background: #f8f9fa; padding: 6px;">Nguyễn Văn A</div>
          <div style="background: #f8f9fa; padding: 6px;">18,500,000</div>
          <div style="background: #e6f4ea; color: #1e8e3e; font-weight: 700; padding: 6px;">24,200,000</div>
          <div style="background: #f8f9fa; padding: 6px;">EMP-02</div>
          <div style="background: #f8f9fa; padding: 6px;">Trần Thị B</div>
          <div style="background: #f8f9fa; padding: 6px;">22,000,000</div>
          <div style="background: #e6f4ea; color: #1e8e3e; font-weight: 700; padding: 6px;">28,600,000</div>
        </div>
        <div style="margin-top: 14px; background: #fef7e0; border: 1px solid #feefc3; padding: 8px 12px; border-radius: 4px; font-size: 0.82rem; font-family: var(--font-mono); color: #b06000;">
          Công thức: =SUM(D2:D100) • Tự động tính toán • Chuẩn OpenXML Excel
        </div>
      `
    },
    slides: {
      title: "Bai_Thuyet_Trinh_Q4.pptx — ZenQ Slides",
      icon: "icon/present.png",
      html: `
        <div style="border: 2px dashed #dadce0; border-radius: 6px; height: 180px; display: flex; flex-direction: column; align-items: center; justify-content: center; background: #f8fafd; text-align: center; padding: 16px;">
          <h4 style="font-size: 1.25rem; font-weight: 800; color: #202124; margin-bottom: 6px;">Chiến Lược Tăng Tốc 2026</h4>
          <p style="color: #5f6368; font-size: 0.88rem;">Khung hình: 1920x1080 (16:9) • Trình chiếu chuyên nghiệp</p>
          <div style="margin-top: 14px; display: flex; gap: 8px;">
            <span style="background: #e8f0fe; color: #1a73e8; padding: 4px 10px; border-radius: 12px; font-size: 0.76rem; font-weight: 600;">Shape Vector</span>
            <span style="background: #fef7e0; color: #ea8600; padding: 4px 10px; border-radius: 12px; font-size: 0.76rem; font-weight: 600;">Presenter View</span>
          </div>
        </div>
      `
    },
    pdf: {
      title: "Hop_Dong_Kinh_Te_Bao_Mat.pdf — ZenQ PDF",
      icon: "icon/pdf.png",
      html: `
        <div style="background: #ffffff; border: 1px solid #dadce0; border-radius: 6px; padding: 18px;">
          <div style="display: flex; justify-content: space-between; border-bottom: 1px solid #f1f3f4; padding-bottom: 8px; margin-bottom: 10px; font-size: 0.8rem; color: #5f6368;">
            <span>Trang 1 / 18</span>
            <span style="color: #d93025; font-weight: 600;">Đã xác thực chữ ký điện tử</span>
          </div>
          <div class="mockup-text-line title" style="background: #d93025; width: 50%;"></div>
          <div class="mockup-text-line w90"></div>
          <div class="mockup-text-line w80"></div>
          <div class="mockup-text-line w90"></div>
          <div class="mockup-text-line w70"></div>
        </div>
      `
    },
    mobile: {
      title: "ZenQ_Mobile_App.apk — Native C# Android",
      icon: "icon/ZENQOFFICE.png",
      html: `
        <div style="max-width: 280px; margin: 0 auto; background: #ffffff; border-radius: 18px; border: 3px solid #0f172a; padding: 14px 10px; box-shadow: 0 4px 14px rgba(0,0,0,0.1);">
          <div style="width: 40px; height: 3px; background: #cbd5e1; border-radius: 3px; margin: 0 auto 10px auto;"></div>
          <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; padding-bottom: 4px; border-bottom: 1px solid #f1f5f9;">
            <span style="font-weight: 700; font-size: 0.78rem; color: #0f172a;">📄 Baocao_A4.docx</span>
            <span style="font-size: 0.65rem; background: #ccfbf1; color: #0f766e; padding: 2px 6px; border-radius: 6px; font-weight: 700;">A4 Chuẩn</span>
          </div>
          <div class="mockup-text-line title" style="height: 8px; margin-bottom: 6px;"></div>
          <div class="mockup-text-line w90" style="height: 6px; margin-bottom: 4px;"></div>
          <div class="mockup-text-line w80" style="height: 6px; margin-bottom: 4px;"></div>
          <div class="mockup-text-line w70" style="height: 6px; margin-bottom: 10px;"></div>
          <div style="background: #f0fdfa; border: 1px solid #99f6e4; border-radius: 6px; padding: 6px; font-size: 0.7rem; color: #0f766e; text-align: center; font-weight: 600;">
            ✓ Động cơ C# Native • Không tràn dòng
          </div>
        </div>
      `
    }
  };


  window.switchSuiteTab = function (targetApp) {
    if (!targetApp) return;

    // Update Tab Buttons
    appTabs.forEach((tab) => {
      if (tab.getAttribute('data-tab') === targetApp) {
        tab.classList.add('active');
      } else {
        tab.classList.remove('active');
      }
    });

    // Update Panels
    appPanels.forEach((panel) => {
      if (panel.getAttribute('data-panel') === targetApp) {
        panel.classList.add('active');
      } else {
        panel.classList.remove('active');
      }
    });

    // Update Mockup Window
    const config = appMockupConfig[targetApp];
    if (config) {
      if (mockupAppIcon) mockupAppIcon.src = config.icon;
      if (mockupDocTitle) mockupDocTitle.textContent = config.title;
      if (mockupSheet) mockupSheet.innerHTML = config.html;
    }
  };

  appTabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      const targetApp = tab.getAttribute('data-tab');
      window.switchSuiteTab(targetApp);
    });
  });

  // Mockup Ribbon Tabs
  window.setMockupTab = function (tabName) {
    const ribbonBtns = document.querySelectorAll('.mockup-ribbon .ribbon-btn');
    ribbonBtns.forEach((b) => b.classList.remove('active'));
    if (event && event.target) {
      event.target.classList.add('active');
    }
  };

  // =========================================================================
  // 4. FAQ ACCORDION & CATEGORY FILTER
  // =========================================================================
  faqItems.forEach((item) => {
    const header = item.querySelector('.faq-header');
    const body = item.querySelector('.faq-body');

    if (header && body) {
      header.addEventListener('click', () => {
        const isOpen = item.classList.contains('open');

        // Close other items
        faqItems.forEach((other) => {
          if (other !== item) {
            other.classList.remove('open');
            const otherBody = other.querySelector('.faq-body');
            if (otherBody) otherBody.style.maxHeight = null;
          }
        });

        if (isOpen) {
          item.classList.remove('open');
          body.style.maxHeight = null;
        } else {
          item.classList.add('open');
          body.style.maxHeight = body.scrollHeight + 'px';
        }
      });
    }
  });

  // Category Filtering
  faqFilterBtns.forEach((btn) => {
    btn.addEventListener('click', () => {
      const category = btn.getAttribute('data-category');

      faqFilterBtns.forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');

      faqItems.forEach((item) => {
        const itemCat = item.getAttribute('data-category');
        if (category === 'all' || itemCat === category) {
          item.style.display = 'block';
        } else {
          item.style.display = 'none';
        }
      });
    });
  });

  // =========================================================================
  // 5. NAVBAR SCROLL EFFECT & ACTIVE SECTION HIGHLIGHT
  // =========================================================================
  const sections = document.querySelectorAll('section[id]');
  window.addEventListener('scroll', () => {
    const scrollY = window.pageYOffset;

    if (navbar) {
      if (scrollY > 20) {
        navbar.classList.add('scrolled');
      } else {
        navbar.classList.remove('scrolled');
      }
    }

    sections.forEach((current) => {
      const sectionHeight = current.offsetHeight;
      const sectionTop = current.offsetTop - 120;
      const sectionId = current.getAttribute('id');

      if (scrollY > sectionTop && scrollY <= sectionTop + sectionHeight) {
        navLinks.forEach((link) => {
          link.classList.remove('active');
          if (link.getAttribute('href') === '#' + sectionId) {
            link.classList.add('active');
          }
        });
      }
    });
  });

  // =========================================================================
  // 6. INITIALIZE ON DOM READY
  // =========================================================================
  document.addEventListener('DOMContentLoaded', () => {
    applyLanguage(currentLang);
  });
})();
