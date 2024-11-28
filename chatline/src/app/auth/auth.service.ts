import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environments } from '../environments/environmets';
import { User } from '../main/interface/user.interface';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { MainServiceService } from '../main/main-service.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private baseUrl = environments.baseUrl;

  constructor(private http: HttpClient, private mainService: MainServiceService) { }

  registerUser(username: string, password: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/register`, { username, password });
  }
  // Check if the user is logged in
  isAuthenticated(): boolean {
    const user = this.mainService.getCurrentUser();
    return !!user;
  }
loginUser(username: string, password: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.baseUrl}/login`, { username, password });
  }

  // Log User In
  LogUser(name: string, passwd: string): Observable<User | null> {
    const loginData = { NickName: name, Passwd: passwd };
    return this.http.post<User>(`${this.baseUrl}/getall`, loginData).pipe(
      tap(user => {
        this.mainService.setCurrentUser(user);
        console.log('User logged in:', user);
      }),
      map(user => user || null),
      catchError(() => of(null))
    );
  }

  // Log User Out
  LogOut(): void {
    this.mainService.clearCurrentUser(); // Clear current user from the service
    localStorage.removeItem('currentUser'); // Clear user data from localStorage
    console.log('User logged out');
  }
}
