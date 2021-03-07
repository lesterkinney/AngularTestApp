import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Member } from '../_models/member';
import { User } from '../_models/user';
import { UserParams } from '../_models/userParams';
import { AccountService } from './account.service';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';

@Injectable({
  providedIn: 'root'
})
export class MembersService {
  url = environment.apiUrl + 'appusers/';
  likesUrl = environment.apiUrl;
  members: Member[] = [];
  memberCache = new Map();
  user: User;
  userParams: UserParams

  constructor(private http: HttpClient, private accountService: AccountService) {
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.user = user;
      this.userParams = new UserParams(user);
    })
  }

  getUserParams() {
    return this.userParams;
  }

  setUserParams(userParams: UserParams) {
    this.userParams = userParams;
  }

  resetUserParams() {
    this.userParams = new UserParams(this.user);
    return this.userParams;
  }

  getMembers(userParams: UserParams) {
var response = this.memberCache.get(Object.values(userParams).join('|'));
if(response) {
  return of(response);
}

    let params = getPaginationHeaders(userParams.pageNumber, userParams.pageSize);
    params = params.append('minAge', userParams.minAge.toString());
    params = params.append('maxAge', userParams.maxAge.toString());
    params = params.append('gender', userParams.gender);
    params = params.append('orderBy', userParams.orderBy);

    return getPaginatedResult<Member[]>(this.url, params, this.http)
      .pipe(map(response => {
        this.memberCache.set(Object.values(userParams).join('|'), response);
        console.log(this.url);
        console.log(response);
        console.log(params);
        return response;
      }));
  }

  getMember(userName: string) {
    const member = [...this.memberCache.values()]
      .reduce((arr, elem) => arr.concat(elem.result), [])
      .find((member: Member) => member.userName === userName);

    if(member) {
      console.log("member cached");
      return of(member);
    }

    return this.http.get<Member>(this.url + userName);
  }

  updateMember(member: Member) {
    return this.http.put(this.url, member).pipe(
      map(() => {
        const index = this.members.indexOf(member);
        this.members[index] = member;
      })
    )
  }

  deletePhoto(photoId: number) {
    return this.http.delete(this.url + 'delete-photo/' + photoId);
  }

  setMainPhoto(photoId: number) {
    return this.http.put(this.url + 'set-main-photo/' + photoId, {});
  }

  addLike(userName: string) {
    return this.http.post(this.likesUrl + 'likes/' + userName, {});
  }

  getLikes(predicate: string, pageNumber: number, pageSize: number) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    params = params.append('predicate', predicate);
    return getPaginatedResult<Partial<Member[]>>(this.likesUrl + 'likes/', params, this.http)
  }
}
