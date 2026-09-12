"use strict";

(function () {
  var sidebarStorageKey = "adminHMD.sidebarMini";
  var themeStorageKey = "adminHMD.colorTheme";
  var desktopMedia = "(min-width: 992px)";

  function onReady(callback) {
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", callback);
      return;
    }

    callback();
  }

  function isDesktop() {
    return window.matchMedia(desktopMedia).matches;
  }

  function canUseStorage() {
    try {
      var testKey = sidebarStorageKey + ".test";
      window.localStorage.setItem(testKey, "1");
      window.localStorage.removeItem(testKey);
      return true;
    } catch (error) {
      return false;
    }
  }

  function getSavedMiniState(storageAvailable) {
    if (!storageAvailable) {
      return false;
    }

    return window.localStorage.getItem(sidebarStorageKey) === "true";
  }

  function saveMiniState(storageAvailable, isMini) {
    if (storageAvailable) {
      window.localStorage.setItem(sidebarStorageKey, String(isMini));
    }
  }

  function getPreferredTheme(storageAvailable) {
    var savedTheme = storageAvailable ? window.localStorage.getItem(themeStorageKey) : "";

    if (savedTheme === "dark" || savedTheme === "light") {
      return savedTheme;
    }

    if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches) {
      return "dark";
    }

    return "light";
  }

  onReady(function () {
    var body = document.body;
    var sidebarToggle = document.querySelector("[data-sidebar-toggle]");
    var themeToggles = document.querySelectorAll("[data-theme-toggle]");
    var themeIcons = document.querySelectorAll("[data-theme-icon]");
    var closeButtons = document.querySelectorAll("[data-sidebar-close]");
    var sidebarLinks = document.querySelectorAll(".sidebar-nav .nav-link");
    var mediaQuery = window.matchMedia(desktopMedia);
    var storageAvailable = canUseStorage();

    function initValidation() {
      var forms = document.querySelectorAll(".needs-validation");

      Array.prototype.forEach.call(forms, function (form) {
        form.addEventListener("submit", function (event) {
          if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
          }

          form.classList.add("was-validated");
        });
      });
    }

    function initTableSearch() {
      var searchInputs = document.querySelectorAll("[data-table-search]");

      Array.prototype.forEach.call(searchInputs, function (input) {
        var tableId = input.getAttribute("data-table-search");
        var table = document.getElementById(tableId);

        if (!table) {
          return;
        }

        input.addEventListener("input", function () {
          var query = input.value.trim().toLowerCase();
          var rows = table.querySelectorAll("tbody tr");

          Array.prototype.forEach.call(rows, function (row) {
            row.hidden = query !== "" && row.textContent.toLowerCase().indexOf(query) === -1;
          });
        });
      });
    }

    function updateThemeControls(theme) {
      var nextTheme = theme === "dark" ? "light" : "dark";
      var label = "Switch to " + nextTheme + " mode";
      var iconClass = theme === "dark" ? "bi bi-sun" : "bi bi-moon-stars";

      Array.prototype.forEach.call(themeToggles, function (button) {
        button.setAttribute("aria-label", label);
        button.setAttribute("title", label);
      });

      Array.prototype.forEach.call(themeIcons, function (icon) {
        icon.className = iconClass;
      });
    }

    function applyTheme(theme) {
      document.documentElement.setAttribute("data-theme", theme);
      document.documentElement.setAttribute("data-bs-theme", theme);

      if (storageAvailable) {
        window.localStorage.setItem(themeStorageKey, theme);
      }

      updateThemeControls(theme);
    }

    function initThemeToggle() {
      applyTheme(getPreferredTheme(storageAvailable));

      Array.prototype.forEach.call(themeToggles, function (button) {
        button.addEventListener("click", function () {
          var currentTheme = document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
          applyTheme(currentTheme === "dark" ? "light" : "dark");
        });
      });
    }

    initValidation();
    initTableSearch();
    initThemeToggle();

    // Initialize user profile values in UI. Provide a window.adminHMDUser object to override defaults.
    function initUserProfile() {
      var user = window.adminHMDUser || { name: "Admin Hasan", workspace: "PetShop", initials: "AH" };

      var sidebarNameEl = document.querySelector(".sidebar-user strong");
      var sidebarWorkspaceEl = document.querySelector(".sidebar-user small");
      var profileNameEls = document.querySelectorAll(".profile-name");
      var avatarInitialsEls = document.querySelectorAll(".admin-avatar-badge span, .avatar-initials");

      if (sidebarNameEl && user.name) sidebarNameEl.textContent = user.name;
      if (sidebarWorkspaceEl && user.workspace) sidebarWorkspaceEl.textContent = user.workspace;
      if (user.initials) {
        Array.prototype.forEach.call(avatarInitialsEls, function (el) {
          el.textContent = user.initials;
        });
      }

      Array.prototype.forEach.call(profileNameEls, function (el) {
        if (user.name) el.textContent = user.name;
      });
    }

    initUserProfile();

    // Initialize notification dropdown state, unread badge counter, and localStorage persistence
    function initNotifications() {
      var notificationStorageKey = "adminHMD.readNotifications";
      var badge = document.getElementById("notificationBadge");
      var headerBadge = document.getElementById("notificationHeaderBadge");
      var statusText = document.getElementById("notificationStatusText");
      var markAllBtn = document.getElementById("markAllNotificationsReadBtn");
      var resetBtn = document.getElementById("resetNotificationsBtn");
      var items = document.querySelectorAll(".notification-menu [data-notif-id]");

      if (!items.length) {
        return;
      }

      function getReadIds() {
        if (!storageAvailable) {
          return [];
        }
        try {
          var raw = window.localStorage.getItem(notificationStorageKey);
          return raw ? JSON.parse(raw) : [];
        } catch (e) {
          return [];
        }
      }

      function saveReadIds(ids) {
        if (!storageAvailable) {
          return;
        }
        try {
          window.localStorage.setItem(notificationStorageKey, JSON.stringify(ids));
        } catch (e) {
          // ignore storage quota issues
        }
      }

      var readIds = getReadIds();

      function updateUI() {
        var unreadCount = 0;
        Array.prototype.forEach.call(items, function (item) {
          var notifId = item.getAttribute("data-notif-id");
          var isRead = readIds.indexOf(notifId) !== -1;
          if (isRead) {
            item.classList.remove("unread");
            item.classList.add("read");
          } else {
            item.classList.remove("read");
            item.classList.add("unread");
            unreadCount++;
          }
        });

        // 1. Bell icon badge (dynamic unread counter)
        if (badge) {
          if (unreadCount > 0) {
            badge.textContent = String(unreadCount);
            badge.style.display = "inline-flex";
          } else {
            badge.style.display = "none";
          }
        }

        // 2. Dropdown header badge
        if (headerBadge) {
          if (unreadCount > 0) {
            headerBadge.textContent = unreadCount + " yeni";
            headerBadge.className = "badge rounded-pill bg-danger-subtle text-danger small px-2 py-1";
          } else {
            headerBadge.textContent = "Hamısı oxunub";
            headerBadge.className = "badge rounded-pill bg-success-subtle text-success small px-2 py-1";
          }
        }

        // 3. Dropdown footer status text
        if (statusText) {
          if (unreadCount > 0) {
            statusText.textContent = unreadCount + " oxunmamış bildiriş";
          } else {
            statusText.textContent = "Oxunmamış bildiriş yoxdur";
          }
        }
      }

      // Mark clicked notification as read in localStorage before following link
      Array.prototype.forEach.call(items, function (item) {
        item.addEventListener("click", function () {
          var notifId = this.getAttribute("data-notif-id");
          if (notifId && readIds.indexOf(notifId) === -1) {
            readIds.push(notifId);
            saveReadIds(readIds);
            updateUI();
          }
        });
      });

      // Mark all notifications as read
      if (markAllBtn) {
        markAllBtn.addEventListener("click", function (e) {
          e.preventDefault();
          e.stopPropagation();
          Array.prototype.forEach.call(items, function (item) {
            var notifId = item.getAttribute("data-notif-id");
            if (notifId && readIds.indexOf(notifId) === -1) {
              readIds.push(notifId);
            }
          });
          saveReadIds(readIds);
          updateUI();
        });
      }

      // Reset notifications for testing or re-activation
      if (resetBtn) {
        resetBtn.addEventListener("click", function (e) {
          e.preventDefault();
          e.stopPropagation();
          readIds = [];
          saveReadIds(readIds);
          updateUI();
        });
      }

      updateUI();
    }

    initNotifications();

    function initGlobalSearch() {
      var searchInput = document.getElementById("adminGlobalSearchInput");
      var searchDropdown = document.getElementById("adminGlobalSearchDropdown");
      var searchResults = document.getElementById("adminGlobalSearchResults");
      var searchSpinner = document.getElementById("adminSearchSpinner");
      var searchWrapper = document.getElementById("adminGlobalSearchWrapper");

      if (!searchInput || !searchDropdown || !searchResults || !searchWrapper) {
        return;
      }

      var debounceTimeout = null;
      var currentQuery = "";

      function hideDropdown() {
        searchDropdown.style.display = "none";
      }

      function showSpinner() {
        if (searchSpinner) searchSpinner.style.display = "inline-block";
      }

      function hideSpinner() {
        if (searchSpinner) searchSpinner.style.display = "none";
      }

      function escapeHtml(str) {
        if (!str) return "";
        return String(str)
          .replace(/&/g, "&amp;")
          .replace(/</g, "&lt;")
          .replace(/>/g, "&gt;")
          .replace(/"/g, "&quot;")
          .replace(/'/g, "&#039;");
      }

      function renderCategory(title, iconClass, badgeClass, items) {
        if (!items || items.length === 0) return "";

        var html = '<div class="global-search-category-header d-flex align-items-center justify-content-between">';
        html += '<span><i class="' + iconClass + ' me-2"></i>' + title + '</span>';
        html += '<span class="badge ' + badgeClass + ' rounded-pill">' + items.length + '</span>';
        html += '</div>';

        items.forEach(function (item) {
          html += '<a href="' + item.url + '" class="global-search-item">';
          html += '  <div class="d-flex align-items-center gap-2 overflow-hidden">';
          html += '    <div class="item-icon ' + badgeClass + '"><i class="' + iconClass + '"></i></div>';
          html += '    <div class="text-truncate">';
          html += '      <div class="item-title text-truncate">' + escapeHtml(item.name) + '</div>';
          if (item.detail) {
            html += '      <div class="item-subtitle text-truncate">' + escapeHtml(item.detail) + '</div>';
          }
          html += '    </div>';
          html += '  </div>';
          html += '  <i class="bi bi-chevron-right text-muted small ms-2"></i>';
          html += '</a>';
        });

        return html;
      }

      function performSearch(query) {
        query = query.trim();
        if (query.length < 2) {
          hideDropdown();
          hideSpinner();
          return;
        }

        currentQuery = query;
        showSpinner();

        fetch("/Admin/Dashboard/GlobalSearch?query=" + encodeURIComponent(query), {
          headers: { "X-Requested-With": "XMLHttpRequest" }
        })
          .then(function (res) {
            if (!res.ok) throw new Error("Search network error");
            return res.json();
          })
          .then(function (data) {
            hideSpinner();

            if (searchInput.value.trim() !== currentQuery) {
              return;
            }

            if (!Array.isArray(data) || data.length === 0) {
              searchResults.innerHTML =
                '<div class="global-search-empty">' +
                '  <i class="bi bi-search fs-3 d-block mb-2 text-muted"></i>' +
                '  <div class="fw-semibold text-body">Nəticə tapılmadı</div>' +
                '  <small class="text-muted">"' + escapeHtml(query) + '" sorğusu üzrə heç bir məhsul, sifariş və ya istifadəçi tapılmadı.</small>' +
                '</div>';
              searchDropdown.style.display = "block";
              return;
            }

            var products = data.filter(function (x) { return x.type === "Product"; });
            var orders = data.filter(function (x) { return x.type === "Order"; });
            var users = data.filter(function (x) { return x.type === "User"; });

            var contentHtml = "";
            contentHtml += renderCategory("Məhsullar", "bi bi-box-seam", "bg-primary-subtle text-primary", products);
            contentHtml += renderCategory("Sifarişlər", "bi bi-receipt", "bg-success-subtle text-success", orders);
            contentHtml += renderCategory("İstifadəçilər", "bi bi-people", "bg-info-subtle text-info", users);

            searchResults.innerHTML = contentHtml;
            searchDropdown.style.display = "block";
          })
          .catch(function (err) {
            console.error("Global search error:", err);
            hideSpinner();
          });
      }

      searchInput.addEventListener("input", function () {
        var val = searchInput.value;
        clearTimeout(debounceTimeout);
        if (val.trim().length < 2) {
          hideDropdown();
          hideSpinner();
          return;
        }

        debounceTimeout = setTimeout(function () {
          performSearch(val);
        }, 300);
      });

      searchInput.addEventListener("focus", function () {
        if (searchInput.value.trim().length >= 2 && searchResults.innerHTML.trim() !== "") {
          searchDropdown.style.display = "block";
        }
      });

      searchInput.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
          hideDropdown();
          searchInput.blur();
        }
      });

      document.addEventListener("click", function (e) {
        if (!searchWrapper.contains(e.target)) {
          hideDropdown();
        }
      });
    }

    initGlobalSearch();

    if (!sidebarToggle) {
      return;
    }

    function setClass(element, className, enabled) {
      if (enabled) {
        element.classList.add(className);
      } else {
        element.classList.remove(className);
      }
    }

    function setToggleExpanded() {
      var expanded = isDesktop()
        ? !body.classList.contains("sidebar-mini")
        : body.classList.contains("sidebar-open");

      sidebarToggle.setAttribute("aria-expanded", String(expanded));
    }

    function closeMobileSidebar() {
      body.classList.remove("sidebar-open");
      setToggleExpanded();
    }

    function toggleSidebar() {
      if (isDesktop()) {
        body.classList.toggle("sidebar-mini");
        saveMiniState(storageAvailable, body.classList.contains("sidebar-mini"));
      } else {
        body.classList.toggle("sidebar-open");
      }

      setToggleExpanded();
    }

    function addCloseHandlers(items) {
      Array.prototype.forEach.call(items, function (item) {
        item.addEventListener("click", function () {
          if (!isDesktop()) {
            closeMobileSidebar();
          }
        });
      });
    }

    if (getSavedMiniState(storageAvailable) && isDesktop()) {
      body.classList.add("sidebar-mini");
    }

    sidebarToggle.addEventListener("click", toggleSidebar);
    addCloseHandlers(closeButtons);
    addCloseHandlers(sidebarLinks);
    setToggleExpanded();

    function handleBreakpointChange() {
      if (isDesktop()) {
        body.classList.remove("sidebar-open");
        setClass(body, "sidebar-mini", getSavedMiniState(storageAvailable));
      } else {
        body.classList.remove("sidebar-mini");
      }

      setToggleExpanded();
    }

    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener("change", handleBreakpointChange);
    } else if (mediaQuery.addListener) {
      mediaQuery.addListener(handleBreakpointChange);
    }
  });
})();
