document.addEventListener("DOMContentLoaded", () => {
    // Menu toggle functionality
    const menuToggles = document.querySelectorAll(".menu-toggle");
    const sidebar = document.querySelector(".sidebar");
    const toggleBtn = document.querySelector(".toggle-sidebar");

    menuToggles.forEach(toggle => {
        toggle.addEventListener("click", function () {
            this.classList.toggle("active");
            const submenu = this.nextElementSibling;

            if (submenu.style.maxHeight) {
                submenu.style.maxHeight = null;
                submenu.classList.remove("active");
            } else {
                submenu.style.maxHeight = submenu.scrollHeight + "px";
                submenu.classList.add("active");
            }

            const icon = this.querySelector('.toggle-icon');
            if (icon) {
                icon.classList.toggle('fa-chevron-down');
                icon.classList.toggle('fa-chevron-up');
            }
        });
    });

    // Sidebar toggle functionality
    if (toggleBtn) {
        toggleBtn.addEventListener("click", () => {
            sidebar.classList.toggle("collapsed");

            // For mobile
            if (window.innerWidth <= 768) {
                sidebar.classList.toggle("show");
            }
        });
    }

    // Auto-collapse submenus when sidebar collapses
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.attributeName === 'class' &&
                mutation.target.classList.contains('collapsed')) {
                document.querySelectorAll('.submenu').forEach(submenu => {
                    submenu.style.maxHeight = null;
                    submenu.classList.remove("active");
                });
                document.querySelectorAll('.menu-toggle').forEach(toggle => {
                    toggle.classList.remove("active");
                    const icon = toggle.querySelector('.toggle-icon');
                    if (icon) {
                        icon.classList.remove('fa-chevron-up');
                        icon.classList.add('fa-chevron-down');
                    }
                });
            }
        });
    });

    if (sidebar) {
        observer.observe(sidebar, { attributes: true });
    }
});

window.addEventListener("resize", () => {
    const sidebar = document.querySelector(".sidebar");
    if (sidebar) {
        sidebar.style.maxHeight = `calc(100vh - ${getHeaderAndFooterHeight()}px)`;
    }
});

function getHeaderAndFooterHeight() {
    const header = document.querySelector("header");
    const footer = document.querySelector("footer");
    return (header?.clientHeight || 0) + (footer?.clientHeight || 0);
}

