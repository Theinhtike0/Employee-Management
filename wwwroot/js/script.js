
        document.addEventListener("DOMContentLoaded", () => {
            const menuToggles = document.querySelectorAll(".menu-toggle");

            menuToggles.forEach(toggle => {
        toggle.addEventListener("click", function () {
            this.classList.toggle("active");
            const submenu = this.nextElementSibling;

            if (submenu.style.maxHeight) {
                submenu.style.maxHeight = null;
            } else {
                submenu.style.maxHeight = submenu.scrollHeight + "px"; 
            }
            const icon = this.querySelector('.toggle-icon');
            if (icon) {
                icon.classList.toggle('fa-chevron-down');
                icon.classList.toggle('fa-chevron-up');
            }
        });
            });
        });