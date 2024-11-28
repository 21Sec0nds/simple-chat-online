import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-auth-page',
  templateUrl: './auth-page.component.html',
})
export class AuthPageComponent {
  loginForm: FormGroup;

  constructor(
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  // Check if the form field is invalid after being touched
  isFieldInvalid(field: string): boolean {
    return !!this.loginForm.controls[field].errors && this.loginForm.controls[field].touched;
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched(); // Mark all fields as touched to show validation errors
      return;
    }

    const { username, password } = this.loginForm.value;
    this.authService.loginUser(username, password).subscribe(isLoggedIn => {
      if (isLoggedIn) {
        this.router.navigate(['/chat']); // Redirect to chat if login is successful
      } else {
        console.log('Invalid login credentials'); // Handle failed login attempt
      }
    });
  }
}
