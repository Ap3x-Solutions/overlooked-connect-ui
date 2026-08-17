/* Overlooked Connect - Internal Operations Platform client script. */

document.addEventListener("DOMContentLoaded", function () { /* [Mozilla Developer Network, [s.a.]] */

    /* Auto-dismiss confirmation alerts after 6 seconds [Bootstrap, [s.a.]] */
    const alertBox = document.querySelector(".alert-dismissible");
    if (alertBox) {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alertBox);
            bsAlert.close();
        }, 6000);
    }

    /* Sidebar toggle for tablet and mobile widths [W3Schools, [s.a.]] */
    const toggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("appSidebar");
    if (toggle && sidebar) {
        toggle.addEventListener("click", function () {
            sidebar.classList.toggle("open"); /* [Mozilla Developer Network, [s.a.]] */
        });
        /* Close the sidebar when the content area is tapped on a small screen */
        document.querySelector(".app-main")?.addEventListener("click", function () {
            if (window.innerWidth <= 1200) { sidebar.classList.remove("open"); }
        });
    }

    /* Bootstrap client-side validation styling [Bootstrap, [s.a.]] */
    document.querySelectorAll("form.needs-validation").forEach(form => {
        form.addEventListener("submit", function (e) {
            if (!this.checkValidity()) { /* [Mozilla Developer Network, [s.a.]] */
                e.preventDefault();
                e.stopPropagation();
            }
            this.classList.add("was-validated");
        });
    });

    /* Severity selector on the staff incident capture screen.
       Severity must be chosen from a controlled list; free text is not accepted, which is the
       rule recorded in the acceptance criteria for US-05. */
    const sevInput = document.getElementById("SeverityValue");
    const sevChips = document.querySelectorAll("[data-severity]");
    sevChips.forEach(chip => {
        chip.addEventListener("click", function () {
            sevChips.forEach(c => c.classList.remove("on-low", "on-med", "on-high", "on-fatal"));
            this.classList.add(this.dataset.tone);
            if (sevInput) { sevInput.value = this.dataset.severity; }

            /* High severity escalates to the Safety Manager immediately (Section 5.2.2). */
            const warn = document.getElementById("severityWarning");
            if (warn) {
                const high = this.dataset.severity === "High" || this.dataset.severity === "Fatal / LTI";
                warn.style.display = high ? "block" : "none";
            }
        });
    });

    /* Character counter on description fields */
    document.querySelectorAll("[data-counter]").forEach(area => {
        const target = document.getElementById(area.dataset.counter);
        const update = () => { if (target) { target.textContent = area.value.length + " / " + (area.maxLength > 0 ? area.maxLength : 500); } };
        area.addEventListener("input", update);
        update();
    });

    /* File inputs show the chosen file name rather than the browser default */
    document.querySelectorAll('input[type="file"]').forEach(input => {
        input.addEventListener("change", function () {
            const label = document.querySelector('[data-file-for="' + this.id + '"]');
            if (label) {
                label.textContent = this.files.length
                    ? Array.from(this.files).map(f => f.name).join(", ")
                    : "No file chosen";
            }
        });
    });

    /* Animate progress bars once on load so percentages read as live values */
    document.querySelectorAll(".pbar > i[data-width]").forEach(bar => {
        requestAnimationFrame(() => { bar.style.width = bar.dataset.width + "%"; });
    });
});

/*
    Reference List:
        - Bootstrap. [s.a.]. Alerts. [online]. Available at: <https://getbootstrap.com/docs/5.3/components/alerts/> [Accessed 14 August 2026].
        - Bootstrap. [s.a.]. Validation. [online]. Available at: <https://getbootstrap.com/docs/5.3/forms/validation/> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. Document: DOMContentLoaded event. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/Document/DOMContentLoaded_event> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. Element: classList property. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/Element/classList> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. EventTarget: addEventListener() method. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/EventTarget/addEventListener> [Accessed 14 August 2026].
        - Mozilla Developer Network. [s.a.]. HTMLFormElement: checkValidity() method. [online]. Available at: <https://developer.mozilla.org/en-US/docs/Web/API/HTMLFormElement/checkValidity> [Accessed 14 August 2026].
        - W3Schools. [s.a.]. How To Create a Side Navigation Menu. [online]. Available at: <https://www.w3schools.com/howto/howto_js_sidenav.asp> [Accessed 14 August 2026].
*/
