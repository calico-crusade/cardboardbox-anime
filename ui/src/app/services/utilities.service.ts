import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class UtilitiesService {

    constructor() { }

    all<T>(data: T[], pred: (item: T) => boolean) {
        for(let i of data) {
            if (!pred(i)) return false;
        }

        return true;
    }

    any<T>(data: T[], item: T) {
        for(let i of data) {
            if (i === item) return true;
        }

        return false;
    }

    anyOf<T>(data: T[], want: T[]) {
        for(let i of data) {
            for(let w of want) {
                if (w === i) return true;
            }
        }

        return false;
    }

    indexInBounds<T>(data: T[], next: number) {
        if (next < 0) return data.length - 1;
        if (next >= data.length) return 0;
        return next;
    }

    ran(max: number, min: number = 0) {
        return Math.floor(Math.random() * max) + min;
    }

    rand<T>(data: T[]) {
        return data[this.ran(data.length)];
    }
}
