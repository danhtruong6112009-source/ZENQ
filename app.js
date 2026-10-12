/**
 * ZENQ OFFICE - OFFICIAL INTERACTION SCRIPT
 * Bilingual Engine, Page Routing, Tabs, Accordion, and Micro-animations
 */

(function () {
  'use strict';

  // State
  let currentLang = localStorage.getItem('zenq_lang') || 'vi';

  // DOM Elements
  const langToggleBtn = document.getElementById('langToggleBtn');
  const navLinks = document.querySelectorAll('.nav-link');
  const appTabs = document.querySelectorAll('.app-tab-btn');
  const appPanels = document.querySelectorAll('.app-tab-content-panel');
  const faqFilterBtns = document.querySelectorAll('.faq-filter-btn');
  const faqItems = document.querySelectorAll('.faq-item');
  const mobileToggle = document.getElementById('mobileMenuToggle');
  const mobileMenu = document.getElementById('mobileNavMenu');

  // =========================================================================
  // 1. BILINGUAL TRANSLATION ENGINE (Tiếng Việt & English Dictionary)
  // =========================================================================
  function applyLanguage(lang) {
    if (!DICTIONARY[lang]) lang = 'vi';
    currentLang = lang;
    localStorage.setItem('zenq_lang', lang);

    document.documentElement.setAttribute('lang', lang);

    // Update all elements with data-i18n
    const elements = document.querySelectorAll('[data-i18n]');
    elements.forEach((el) => {
      const key = el.getAttribute('data-i18n');
      if (DICTIONARY[lang][key]) {
        if (el.tagName === 'INPUT' && el.getAttribute('type') === 'text') {
          el.placeholder = DICTIONARY[lang][key];
        } else {
          el.innerHTML = DICTIONARY[lang][key];
        }
      }
    });

    // Update Language Toggle Button Text
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
  }

  if (langToggleBtn) {
    langToggleBtn.addEventListener('click', () => {
      const newLang = currentLang === 'vi' ? 'en' : 'vi';
      applyLanguage(newLang);
    });
  }

  // =========================================================================
  // 2. PRODUCT SUITE TABS SWITCHER
  // =========================================================================
  appTabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      const targetApp = tab.getAttribute('data-tab');

      // Update Tab button states
      appTabs.forEach((t) => t.classList.remove('active'));
      tab.classList.add('active');

      // Update Panel visibility
      appPanels.forEach((panel) => {
        if (panel.getAttribute('data-panel') === targetApp) {
          panel.classList.add('active');
        } else {
          panel.classList.remove('active');
        }
      });
    });
  });

  // =========================================================================
  // 3. FAQ ACCORDION & CATEGORY FILTER
  // =========================================================================
  faqItems.forEach((item) => {
    const header = item.querySelector('.faq-header');
    const body = item.querySelector('.faq-body');

    if (header && body) {
      header.addEventListener('click', () => {
        const isOpen = item.classList.contains('open');

        // Optional: close other accordions in the same list
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
  // 4. ACTIVE NAVIGATION LINK ON SCROLL
  // =========================================================================
  const sections = document.querySelectorAll('section[id]');
  window.addEventListener('scroll', () => {
    const scrollY = window.pageYOffset;

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
  // 5. METRIC NUMBER TICKER ANIMATION (Intersection Observer)
  // =========================================================================
  const metricsSection = document.querySelector('.metrics-section');
  let metricsAnimated = false;

  if (metricsSection) {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting && !metricsAnimated) {
            metricsAnimated = true;
            animateMetrics();
          }
        });
      },
      { threshold: 0.3 }
    );
    observer.observe(metricsSection);
  }

  function animateMetrics() {
    const counters = document.querySelectorAll('.metric-number');
    counters.forEach((counter) => {
      counter.style.opacity = '0';
      counter.style.transform = 'translateY(10px)';
      counter.style.transition = 'all 0.6s cubic-bezier(0.16, 1, 0.3, 1)';
      setTimeout(() => {
        counter.style.opacity = '1';
        counter.style.transform = 'translateY(0)';
      }, 100);
    });
  }

  // =========================================================================
  // 6. CARD MOUSE GLOW EFFECT (Linear / Apple style)
  // =========================================================================
  const glowCards = document.querySelectorAll('.metric-card, .arch-card, .value-card, .about-card');
  glowCards.forEach((card) => {
    card.addEventListener('mousemove', (e) => {
      const rect = card.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      card.style.setProperty('--mouse-x', `${x}px`);
      card.style.setProperty('--mouse-y', `${y}px`);
    });
  });

  // =========================================================================
  // 7. MOBILE MENU DRAWER
  // =========================================================================
  if (mobileToggle && mobileMenu) {
    mobileToggle.addEventListener('click', () => {
      mobileMenu.classList.toggle('active');
    });

    const mobileLinks = mobileMenu.querySelectorAll('a');
    mobileLinks.forEach((l) => {
      l.addEventListener('click', () => {
        mobileMenu.classList.remove('active');
      });
    });
  }

  // Initial translation boot (priority: Vietnamese)
  document.addEventListener('DOMContentLoaded', () => {
    applyLanguage(currentLang);
  });
})();
