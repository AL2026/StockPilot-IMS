using System.ComponentModel.DataAnnotations;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);

                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(model);
                }

                var existingUsername = await _userManager.FindByNameAsync(model.UserName);

                if (existingUsername != null)
                {
                    ModelState.AddModelError("UserName", "This username is already taken.");
                    return View(model);
                }

                var user = new IdentityUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Staff");
                    await SendVerificationCodeAsync(user);

                    TempData["SuccessMessage"] =
                        $"Almost there! We've sent a verification code to {model.Email}.";

                    return RedirectToAction(
                        nameof(VerifyEmail),
                        new { email = model.Email });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult VerifyEmail(string? email = null)
        {
            return View(new VerifyEmailViewModel
            {
                Email = email ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid code or email.");
                return View(model);
            }

            if (user.EmailConfirmed)
            {
                TempData["SuccessMessage"] =
                    "Your email is already verified - you can log in.";

                return RedirectToAction(nameof(Login));
            }

            var verificationCode = await _context.EmailVerificationCodes
                .Where(c =>
                    c.UserId == user.Id &&
                    c.Code == model.Code &&
                    !c.IsUsed)
                .OrderByDescending(c => c.ExpiresAt)
                .FirstOrDefaultAsync();

            if (verificationCode == null ||
                verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This code is invalid or has expired. Request a new one below.");

                return View(model);
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            verificationCode.IsUsed = true;
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] =
                $"Welcome, {user.UserName}! Your email is verified.";

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationCode(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null && !user.EmailConfirmed)
            {
                await SendVerificationCodeAsync(user);
            }

            TempData["SuccessMessage"] =
                "If that account needs verifying, a new code has been sent.";

            return RedirectToAction(
                nameof(VerifyEmail),
                new { email });
        }

        private async Task SendVerificationCodeAsync(IdentityUser user)
        {
            var oldCodes = _context.EmailVerificationCodes
                .Where(c => c.UserId == user.Id && !c.IsUsed);

            _context.EmailVerificationCodes.RemoveRange(oldCodes);

            var code = Random.Shared.Next(10000, 99999).ToString();

            _context.EmailVerificationCodes.Add(new EmailVerificationCode
            {
                UserId = user.Id,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            });

            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Verify your StockPilot account",
                $"<p>Your verification code is:</p>" +
                $"<h2 style=\"letter-spacing:4px;\">{code}</h2>" +
                $"<p>Enter this on the verification page to activate your account. " +
                $"This code expires in 15 minutes.</p>");
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // Check the password first so unconfirmed users don't trigger
                    // the verification flow when they entered a wrong password.
                    var passwordCorrect =
                        await _userManager.CheckPasswordAsync(
                            user,
                            model.Password);

                    if (passwordCorrect &&
                        !await _userManager.IsEmailConfirmedAsync(user))
                    {
                        await SendVerificationCodeAsync(user);

                        TempData["ErrorMessage"] =
                            "Please verify your email before logging in - " +
                            "we just sent you a new code.";

                        return RedirectToAction(
                            nameof(VerifyEmail),
                            new { email = model.Email });
                    }

                    if (passwordCorrect)
                    {
                        var result =
                            await _signInManager.PasswordSignInAsync(
                                user,
                                model.Password,
                                model.RememberMe,
                                lockoutOnFailure: false);

                        if (result.Succeeded)
                        {
                            if (!string.IsNullOrEmpty(returnUrl) &&
                                Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }

                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var oldCodes = _context.PasswordResetCodes
                    .Where(c => c.UserId == user.Id && !c.IsUsed);

                _context.PasswordResetCodes.RemoveRange(oldCodes);

                var code = Random.Shared.Next(10000, 99999).ToString();

                _context.PasswordResetCodes.Add(new PasswordResetCode
                {
                    UserId = user.Id,
                    Code = code,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false
                });

                await _context.SaveChangesAsync();

                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Your StockPilot password reset code",
                    $"<p>Your password reset code is:</p>" +
                    $"<h2 style=\"letter-spacing:4px;\">{code}</h2>" +
                    $"<p>This code expires in 15 minutes. " +
                    $"If you didn't request this, you can safely ignore this email.</p>");
            }

            TempData["SuccessMessage"] =
                "If an account exists for that email, a reset code has been sent.";

            return RedirectToAction(
                nameof(ResetPassword),
                new { email = model.Email });
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string? email = null)
        {
            return View(new ResetPasswordViewModel
            {
                Email = email ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid code or email.");

                return View(model);
            }

            var resetCode = await _context.PasswordResetCodes
                .Where(c =>
                    c.UserId == user.Id &&
                    c.Code == model.Code &&
                    !c.IsUsed)
                .OrderByDescending(c => c.ExpiresAt)
                .FirstOrDefaultAsync();

            if (resetCode == null ||
                resetCode.ExpiresAt < DateTime.UtcNow)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This code is invalid or has expired. Request a new one.");

                return View(model);
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            resetCode.IsUsed = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your password has been reset. You can now log in.";

            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new ProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Staff"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var roles = await _userManager.GetRolesAsync(user);
            model.Role = roles.FirstOrDefault() ?? "Staff";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usernameChanged =
                !string.Equals(
                    user.UserName,
                    model.UserName,
                    StringComparison.OrdinalIgnoreCase);

            var emailChanged =
                !string.Equals(
                    user.Email,
                    model.Email,
                    StringComparison.OrdinalIgnoreCase);

            if (usernameChanged)
            {
                var existingUsername =
                    await _userManager.FindByNameAsync(model.UserName);

                if (existingUsername != null &&
                    existingUsername.Id != user.Id)
                {
                    ModelState.AddModelError(
                        "UserName",
                        "This username is already taken.");

                    return View(model);
                }
            }

            if (emailChanged)
            {
                var existingEmail =
                    await _userManager.FindByEmailAsync(model.Email);

                if (existingEmail != null &&
                    existingEmail.Id != user.Id)
                {
                    ModelState.AddModelError(
                        "Email",
                        "An account with this email already exists.");

                    return View(model);
                }
            }

            if (usernameChanged)
            {
                var setUserNameResult =
                    await _userManager.SetUserNameAsync(
                        user,
                        model.UserName);

                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }
            }

            if (emailChanged)
            {
                var setEmailResult =
                    await _userManager.SetEmailAsync(
                        user,
                        model.Email);

                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }

                user.EmailConfirmed = false;
                await _userManager.UpdateAsync(user);
                await SendVerificationCodeAsync(user);

                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] =
                    "Profile updated. Since your email changed, please verify it - " +
                    "we sent a new code.";

                return RedirectToAction(
                    nameof(VerifyEmail),
                    new { email = model.Email });
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] =
                "Profile updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                TempData["PasswordErrors"] =
                    string.Join(
                        "|",
                        ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage));

                return RedirectToAction(nameof(Profile));
            }

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                TempData["PasswordErrors"] =
                    string.Join(
                        "|",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Profile));
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] =
                "Password changed successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class RegisterViewModel
    {
        [Required]
        [StringLength(
            30,
            ErrorMessage = "The {0} must be between {2} and {1} characters.",
            MinimumLength = 3)]
        [RegularExpression(
            @"^[a-zA-Z0-9_.]+$",
            ErrorMessage = "Username can only contain letters, numbers, underscores and periods.")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [RegularExpression(
            @"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Please enter a valid email address (e.g. name@example.com).")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            ErrorMessage = "The {0} must be at least {2} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare(
            "Password",
            ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [RegularExpression(
            @"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class VerifyEmailViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            5,
            MinimumLength = 5,
            ErrorMessage = "The code must be exactly 5 digits.")]
        [RegularExpression(
            @"^\d{5}$",
            ErrorMessage = "The code must be 5 digits.")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [RegularExpression(
            @"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            5,
            MinimumLength = 5,
            ErrorMessage = "The code must be exactly 5 digits.")]
        [RegularExpression(
            @"^\d{5}$",
            ErrorMessage = "The code must be 5 digits.")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            ErrorMessage = "The {0} must be at least {2} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(
            "NewPassword",
            ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        [Required]
        [StringLength(
            30,
            ErrorMessage = "The {0} must be between {2} and {1} characters.",
            MinimumLength = 3)]
        [RegularExpression(
            @"^[a-zA-Z0-9_.]+$",
            ErrorMessage = "Username can only contain letters, numbers, underscores and periods.")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [RegularExpression(
            @"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            ErrorMessage = "The {0} must be at least {2} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(
            "NewPassword",
            ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
