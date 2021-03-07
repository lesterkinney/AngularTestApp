import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { User } from '../_models/user';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl + 'admin/';

  constructor(private http: HttpClient) { }

  getUsersWithRoles() {
    return this.http.get<Partial<User[]>>(this.baseUrl + "users-with-roles");
  }

  updateUserRoles(userName: string, roles: string[]){
    return this.http.post(this.baseUrl + "edit-roles/" + userName + '?roles=' + roles, {})
  }
}
