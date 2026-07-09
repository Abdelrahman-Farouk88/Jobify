// ── Jobify v3 Mission Control — UI Interactions ────────────
(function () {
  'use strict';

  // Navbar scroll
  const nav = document.querySelector('.jobify-nav');
  if (nav) {
    const onScroll = () => nav.classList.toggle('scrolled', window.scrollY > 24);
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
  }

  // Scroll-reveal
  if ('IntersectionObserver' in window) {
    const io = new IntersectionObserver((entries) => {
      entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('revealed'); io.unobserve(e.target); } });
    }, { threshold: 0.1 });
    document.querySelectorAll('.reveal').forEach(el => io.observe(el));
  } else {
    document.querySelectorAll('.reveal').forEach(el => el.classList.add('revealed'));
  }

  // Stagger cards
  document.querySelectorAll('.feature-grid .feature-card').forEach((card, i) => {
    card.classList.add('reveal');
    card.style.transitionDelay = (i * 0.08) + 's';
  });

  // Animated counter
  function countUp(el, target, duration) {
    duration = duration || 1200;
    var start = performance.now();
    var fmt = new Intl.NumberFormat();
    function run(now) {
      var p = Math.min((now - start) / duration, 1);
      var eased = 1 - Math.pow(1 - p, 3);
      el.textContent = fmt.format(Math.round(eased * target));
      if (p < 1) requestAnimationFrame(run);
    }
    requestAnimationFrame(run);
  }
  if ('IntersectionObserver' in window) {
    document.querySelectorAll('[data-count]').forEach(function(el) {
      var target = parseInt(el.dataset.count, 10);
      var io2 = new IntersectionObserver(function(entries) {
        if (entries[0].isIntersecting) { countUp(el, target); io2.disconnect(); }
      }, { threshold: 0.5 });
      io2.observe(el);
    });
  }

  // Score bar animate
  if ('IntersectionObserver' in window) {
    document.querySelectorAll('.score-bar__fill[data-width]').forEach(function(bar) {
      bar.style.width = '0%';
      var io3 = new IntersectionObserver(function(entries) {
        if (entries[0].isIntersecting) {
          requestAnimationFrame(function() { bar.style.width = bar.dataset.width + '%'; });
          io3.disconnect();
        }
      }, { threshold: 0.5 });
      io3.observe(bar);
    });
  }

  // Match ring conic gradient
  document.querySelectorAll('.match-ring[data-score]').forEach(function(ring) {
    var score = parseInt(ring.dataset.score, 10);
    var pct = Math.min(score, 100);
    var color1 = pct >= 70 ? '#00d4ff' : (pct >= 40 ? '#a855f7' : '#ffb800');
    var color2 = pct >= 70 ? '#00ff88' : (pct >= 40 ? '#00d4ff' : '#ff4757');
    ring.style.background = 'conic-gradient(' + color1 + ' 0%, ' + color2 + ' ' + pct + '%, #1e3550 ' + pct + '%)';
  });

  // Drag & drop file
  document.querySelectorAll('.dropzone').forEach(function(zone) {
    var input = zone.querySelector('input[type="file"]');
    zone.addEventListener('dragover', function(e) { e.preventDefault(); zone.classList.add('drag-over'); });
    zone.addEventListener('dragleave', function() { zone.classList.remove('drag-over'); });
    zone.addEventListener('drop', function(e) {
      e.preventDefault(); zone.classList.remove('drag-over');
      if (input && e.dataTransfer.files.length) {
        try { var dt = new DataTransfer(); dt.items.add(e.dataTransfer.files[0]); input.files = dt.files; } catch(ex){}
        var name = zone.querySelector('.dropzone__filename');
        if (name) name.textContent = e.dataTransfer.files[0].name;
      }
    });
    zone.addEventListener('click', function() { if (input) input.click(); });
    if (input) {
      input.addEventListener('change', function() {
        var name = zone.querySelector('.dropzone__filename');
        if (name && input.files[0]) name.textContent = input.files[0].name;
      });
    }
  });

  // Active nav highlight
  var path = window.location.pathname.toLowerCase();
  document.querySelectorAll('.nav-pill').forEach(function(link) {
    var href = (link.getAttribute('href') || '').toLowerCase();
    if (href && href !== '/' && path.startsWith(href)) {
      link.style.color = 'var(--j-cyan)';
      link.style.background = 'var(--j-cyan-dim)';
    }
  });

})();

// Chatbot functionality
function toggleChatbot() {
    const panel = document.getElementById('chatbot-panel');
    if (panel.classList.contains('d-none')) {
        panel.classList.remove('d-none');
        document.getElementById('chatbot-input-text').focus();
    } else {
        panel.classList.add('d-none');
    }
}

async function handleChatbotSubmit(event) {
    event.preventDefault();
    const input = document.getElementById('chatbot-input-text');
    const text = input.value.trim();
    if (!text) return;
    
    // Add User Message
    addMessage(text, 'user-message');
    input.value = '';
    
    // Add temporary loading message
    const loadingId = addMessage('...', 'bot-message');
    
    try {
        const response = await fetch('/api/ChatBot/Ask', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ message: text })
        });
        
        if (response.ok) {
            const data = await response.json();
            document.getElementById(loadingId).remove();
            
            let replyText = data.text;
            if (data.jobs && data.jobs.length > 0) {
                replyText += '<ul class="mt-2 mb-0 ps-3">';
                data.jobs.forEach(job => {
                    replyText += `<li><a href="/Jobs/Details/${job.id}" target="_blank">${job.title} at ${job.employer}</a></li>`;
                });
                replyText += '</ul>';
            }
            
            addMessage(replyText, 'bot-message', true);
        } else {
            throw new Error('Server returned error');
        }
    } catch (err) {
        document.getElementById(loadingId).remove();
        addMessage('Sorry, I am having trouble connecting right now.', 'bot-message');
    }
}

function addMessage(text, className, isHtml = false) {
    const messages = document.getElementById('chatbot-messages');
    const msgDiv = document.createElement('div');
    msgDiv.className = `chatbot-message ${className}`;
    msgDiv.id = 'msg-' + Date.now();
    
    if (isHtml) {
        msgDiv.innerHTML = text;
    } else {
        msgDiv.textContent = text;
    }
    
    messages.appendChild(msgDiv);
    messages.scrollTop = messages.scrollHeight;
    return msgDiv.id;
}
