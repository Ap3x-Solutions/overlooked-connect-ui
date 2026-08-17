    /* Auto-dismiss alerts after 5 seconds  [Bootstrap, [s.a.]] */
document.addEventListener("DOMContentLoaded", function () { /* [Mozilla Developer Network, [s.a.]] */
        // Auto-close alert messages
        const alertBox = document.querySelector(".alert");
        if (alertBox) {
            setTimeout(() => {
                const bsAlert = new bootstrap.Alert(alertBox);
                bsAlert.close();
            }, 5000);
        }

    /* Added active class to current nav item  */
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && (currentPath === href || (href !== '/' && currentPath.startsWith(href)))) {
            link.classList.add('active'); /* [Mozilla Developer Network, [s.a.]] */
        }
    });

    /* Form validation enhancement - added validation icons  [Bootstrap, [s.a.]] */
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            if (!this.checkValidity()) { /* [Mozilla Developer Network, [s.a.]] */
                e.preventDefault();
                e.stopPropagation();
            }
            this.classList.add('was-validated');
        });
    });

    /* File input enhancement - shows selected file name */
    const fileInputs = document.querySelectorAll('input[type="file"]');
    fileInputs.forEach(input => {
        input.addEventListener('change', function () {
            const fileName = this.files[0]?.name || 'No file chosen';
            const label = this.closest('.mb-3')?.querySelector('.form-label');
            if (label) {
                const fileSpan = label.querySelector('.file-name');
                if (fileSpan) {
                    fileSpan.textContent = fileName;
                } else {
                    label.innerHTML += ` <span class="file-name text-muted small">(${fileName})</span>`;
                }
            }
        });
    });

    /* Smooth scroll for anchor links */
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href !== '#') {
                const target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({ behavior: 'smooth' }); /* [Mozilla Developer Network, [s.a.]] */
                }
            }
        });
    });
});


/*
    Reference List:
        - Bootstrap. [s.a.]. Alerts. [online]. Available at: <https://getbootstrap.com/docs/5.3/components/alerts/> [Accessed 14 August 2026].
        - Bootstrap. [s.a.]. Validation. [online]. Available at: <https://getbootstrap.com/docs/5.3/forms/validation/> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. Document: DOMContentLoaded event. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/Document/DOMContentLoaded_event> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. Element: classList property. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/Element/classList> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. Element: scrollIntoView() method. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView> [Accessed 14 August 2026].      
        - Mozilla Developer Network. [s.a.]. EventTarget:addEventListener() method. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/EventTarget/addEventListener> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. HTMLFormElement:checkValidity() method. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/HTMLFormElement/checkValidity> [Accessed 14 August 2026].
*/