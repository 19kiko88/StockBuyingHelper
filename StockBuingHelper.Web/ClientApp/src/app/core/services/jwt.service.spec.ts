import { TestBed } from '@angular/core/testing';

import { JwtInfoService } from './jwt-info.service';

describe('JwtService', () => {
  let service: JwtInfoService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(JwtInfoService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
