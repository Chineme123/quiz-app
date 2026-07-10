import { Link } from 'react-router';
import { FullPageMessage } from './FullPageMessage';

export function NotFound() {
  return (
    <FullPageMessage title="We couldn't find that page">
      <Link to="/profile">Back to Quiztin</Link>
    </FullPageMessage>
  );
}
