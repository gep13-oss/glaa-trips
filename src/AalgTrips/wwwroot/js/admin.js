(() => {

    // Progress feedback on submit — disable the submit button and swap its label
    // for the data-progress text. Applies to every admin form that opts in with a
    // data-progress submit, not just the first form on the page (the create/edit/
    // rename/upload forms now each live in their own modal dialog).
    document.querySelectorAll("form").forEach((form) => {
        form.addEventListener("submit", (e) => {
            const elm = e.target;

            if (elm.checkValidity && elm.checkValidity()) {
                const input = elm.querySelector("[data-progress]");

                if (input) {
                    input.disabled = true;
                    input.value = input.getAttribute("data-progress");
                }
            }
        });
    });

    // Modal dialogs — a trigger carrying data-open-dialog="#id" opens the matching
    // <dialog> as a modal; the Escape key closes it natively. Opening from inside
    // the Actions menu also collapses that menu so it is not left hanging open
    // behind the modal.
    document.querySelectorAll("[data-open-dialog]").forEach((trigger) => {
        trigger.addEventListener("click", () => {
            const dialog = document.querySelector(trigger.getAttribute("data-open-dialog"));

            if (dialog && typeof dialog.showModal === "function") {
                const menu = trigger.closest("details[open]");

                if (menu) {
                    menu.open = false;
                }

                dialog.showModal();
            }
        });
    });

    // Any Cancel / close control inside a dialog closes it.
    document.querySelectorAll("[data-close-dialog]").forEach((closer) => {
        closer.addEventListener("click", () => {
            const dialog = closer.closest("dialog");

            if (dialog) {
                dialog.close();
            }
        });
    });

    // Clicking the backdrop (outside the form panel) closes the dialog. The click
    // lands on the <dialog> itself only when it hits the backdrop, since the form
    // fills the panel.
    document.querySelectorAll("dialog.modal").forEach((dialog) => {
        dialog.addEventListener("click", (e) => {
            if (e.target === dialog) {
                dialog.close();
            }
        });
    });

    // Delete album
    const deletealbum = document.querySelector("#deletealbum");

    if (deletealbum) {
        deletealbum.addEventListener("click", (e) => {
            if (!confirm("Are you sure you want to delete the album?")) {
                e.preventDefault();
            }
        }, false);
    }

    // Delete photo
    const deletephoto = document.querySelector("#deletephoto");

    if (deletephoto) {
        deletephoto.addEventListener("click", (e) => {
            if (!confirm("Are you sure you want to delete the photo?")) {
                e.preventDefault();
            }
        }, false);
    }

    // Delete photo from the album grid (one small form per thumbnail)
    document.querySelectorAll(".thumb__delete").forEach((deleteForm) => {
        deleteForm.addEventListener("submit", (e) => {
            if (!confirm("Are you sure you want to delete this photo?")) {
                e.preventDefault();
            }
        });
    });
})();
