import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environments } from '../environments/environmets';
import { User } from './interface/user.interface';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class MainServiceService {
  private currentUser: User | null = null;
  private baseUrl = environments.baseUrl;

  constructor(private http: HttpClient) {}

  // Get the current user from local storage or the current instance
  getCurrentUser(): User | null {
    if (!this.currentUser) {
      const storedUser = localStorage.getItem('currentUser');
      if (storedUser) {
        this.currentUser = JSON.parse(storedUser);
      }
    }
    return this.currentUser;
  }

  // Set a new user in local storage and the current instance
  setCurrentUser(user: User): void {
    this.currentUser = user;
    localStorage.setItem('currentUser', JSON.stringify(user));
  }

  // Get username of the current user
  getUsername(): string {
    const user = this.getCurrentUser();
    return user ? user.nickName : '';
  }

  // Get avatar URL of the current user
  getImgUser(): string {
    const user = this.getCurrentUser();
    return user ? user.avatarUrl : '';
  }

  // Clear the user from local storage
  clearCurrentUser(): void {
    this.currentUser = null;
    localStorage.removeItem('currentUser');
  }

  // Check and update user information
  checkAndUpdateUser(user: User): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/getuser/${user.id}`).pipe(
      map(existingUser => {
        if (existingUser && existingUser.nickName !== user.nickName) {
          user.nickName = existingUser.nickName;
          this.setCurrentUser(user);
        }
        return user;
      }),
      catchError((error) => {
        console.error('Error checking user:', error);
        return throwError(() => new Error('User not found or error occurred.'));
      })
    );
  }

  // Check and update user avatar
  checkAndUpdateUserImg(user: User): Observable<User> {
    return this.http.get<string>(`${this.baseUrl}/getimg/${user.id}`, { responseType: 'text' as 'json' }).pipe(
      map((existingImgUrl: string) => {
        if (existingImgUrl && existingImgUrl !== user.avatarUrl) {
          user.avatarUrl = existingImgUrl;
          this.setCurrentUser(user);
        }
        return user;
      }),
      catchError((error) => {
        console.error('Error checking user image:', error);
        return throwError(() => new Error('User image not found or error occurred.'));
      })
    );
  }

  // Update user information
  updateUser(user: User): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/update/${user.id}`, user).pipe(
      map((updatedUser) => {
        this.setCurrentUser(updatedUser);
        return updatedUser;
      }),
      catchError((error) => {
        console.error('Error updating user:', error);
        return throwError(() => new Error('Error updating user.'));
      })
    );
  }

  // Delete user
  deleteUser(userId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/delete/${userId}`).pipe(
      catchError((error) => {
        console.error('Error deleting user:', error);
        return throwError(() => new Error('Error deleting user.'));
      })
    );
  }

  // Get avatar URL of the user
  getAvatarUrl(userId: number): Observable<string> {
    return this.http.get<string>(`${this.baseUrl}/getimg/${userId}`, { responseType: 'text' as 'json' });
  }

  // Login user
  loginUser(email: string, password: string): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}/login`, { email, password }).pipe(
      map((user) => {
        this.setCurrentUser(user);
        return user;
      }),
      catchError((error) => {
        console.error('Error logging in:', error);
        return throwError(() => new Error('Invalid credentials.'));
      })
    );
  }

  // Register new user
  registerUser(user: User): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}/register`, user).pipe(
      map((newUser) => {
        this.setCurrentUser(newUser);
        return newUser;
      }),
      catchError((error) => {
        console.error('Error registering user:', error);
        return throwError(() => new Error('Error registering user.'));
      })
    );
  }
}
