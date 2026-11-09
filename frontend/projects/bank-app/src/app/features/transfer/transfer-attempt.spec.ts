import { TransferRequest } from '../../data-access/models/banking.models';
import { TransferAttempt } from './transfer-attempt';

const request: TransferRequest = {
  sourceAccountId: 1,
  targetIban: 'TR330006100519786457841326',
  recipientName: 'Ayşe Yılmaz',
  amount: 100,
  description: 'Kira',
};

describe('TransferAttempt', () => {
  const attempt = () => {
    let n = 0;
    return new TransferAttempt(() => `key-${++n}`);
  };

  it('keeps the key across retries of the same transfer', () => {
    const transfer = attempt();

    expect(transfer.keyFor(request)).toBe(transfer.keyFor({ ...request }));
  });

  it('uses a new key once the details change', () => {
    const transfer = attempt();
    const first = transfer.keyFor(request);

    expect(transfer.keyFor({ ...request, amount: 200 })).not.toBe(first);
  });

  it('treats an identical transfer after a success as a new one', () => {
    const transfer = attempt();
    const first = transfer.keyFor(request);

    transfer.finish();

    expect(transfer.keyFor(request)).not.toBe(first);
  });
});
