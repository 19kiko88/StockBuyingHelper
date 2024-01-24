import { TestBed } from '@angular/core/testing';

import { SbhService } from './sbh.service';

describe('SbhService', () => {
  let service: SbhService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SbhService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
