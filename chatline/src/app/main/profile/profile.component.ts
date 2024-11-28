import { Component, OnInit } from '@angular/core';
import { MainServiceService } from '../main-service.service';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environments } from '../../environments/environmets';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styles: [`
    /* Add your styles here */
  `]
})
export class ProfileComponent implements OnInit {
  currentname: string = '';
  newName: string = '';
  userImageUrl: string = '';
  tempImageUrl: string = '';
  private baseUrl = environments.baseUrl;

  constructor(
    private http: HttpClient,
    private service: MainServiceService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.updateCurrentName();
    this.loadAvatar();
  }

  // Update the current username on component load
  updateCurrentName(): void {
    this.currentname = this.service.getUsername();
    if (!this.currentname) {
      this.service.clearCurrentUser();
    }
  }

  // Load the current avatar URL for the user
  loadAvatar(): void {
    const user = this.service.getCurrentUser();
    if (user && user.id !== undefined) {
      this.service.getAvatarUrl(user.id).subscribe(
        (url: string) => {
          console.log('Avatar URL:', url);
          this.userImageUrl = url;
        },
        (error) => {
          console.error('Error fetching avatar URL:', error);
        }
      );
    } else {
      console.error('User ID is undefined or user is not set');
    }
  }

  // Submit the new username
  submit(): void {
    const user = this.service.getCurrentUser();
    if (user && this.newName) {
      user.nickName = this.newName;
      this.http.put(`${this.baseUrl}/update/${user.id}`, user)
        .subscribe(
          () => {
            this.currentname = this.newName;
            this.router.navigate(['/chat']);
          },
          (error) => {
            console.error('Error updating user:', error);
          }
        );
    } else {
      console.log('Username is not defined or new name is empty');
    }
  }

  // Submit the new avatar URL
  submitImageUrl(): void {
    const user = this.service.getCurrentUser();
    if (user && this.userImageUrl) {
      const avatarUpdate = { avatarUrl: this.userImageUrl };

      this.http.put(`${this.baseUrl}/editimg/${user.id}`, avatarUpdate)
        .subscribe(
          () => {
            console.log('User avatar updated successfully');
            this.loadAvatar();  // Reload avatar to reflect the changes
          },
          (error) => {
            console.error('Error updating user avatar:', error);
          }
        );
    } else {
      console.error('User not found or image URL is empty');
    }
  }

  // Handle account deletion
  submitOnDelete(): void {
    const user = this.service.getCurrentUser();
    if (user) {
      const confirmation = window.confirm('Are you sure you want to delete your account? This action cannot be undone.');
      if (confirmation) {
        this.http.delete(`${this.baseUrl}/delete/${user.id}`)
          .subscribe(
            () => {
              this.router.navigate(['/auth']);
            },
            (error) => {
              console.error('Error deleting user:', error);
            }
          );
      }
    }
  }

  updateImageUrl(): void {
    if (this.tempImageUrl.trim()) {
      this.userImageUrl = this.tempImageUrl;
      this.submitImageUrl();
    }
  }
}
