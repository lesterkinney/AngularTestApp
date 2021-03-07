import { Photo } from './photo';

export interface Member {
    id: number;
    userName: string;
    age: number;
    knownAs: string;
    createdDate: Date;
    lastActive: Date;
    gender: string;
    introduction: string;
    lookingFor: string;
    interests: string;
    city: string;
    country: string;
    photoUrl: string;
    photos: Photo[];
  }
  
